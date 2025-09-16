using System;
using System.Collections.Generic;

using UnityEngine.Extension;

namespace UIFramework
{
    public class TransitionManager
    {
        private readonly struct Params : IEquatable<Params>
        {
            public readonly VisibilityTransitionParams VisibilityTransition;
            public readonly IWidget Source;
            public readonly IWidget Target;
            private readonly TimeMode _timeMode;

            public Params(in VisibilityTransitionParams visibilityTransition, IWindow source, IWindow target, TimeMode timeMode)
            {
                VisibilityTransition = visibilityTransition;
                Source = source;
                Target = target;
                _timeMode = timeMode;
            }

            public override bool Equals(object obj)
            {
                return obj is Params other && Equals(other);
            }

            public bool Equals(Params other)
            {
                return VisibilityTransition == other.VisibilityTransition && Source == other.Source && Target == other.Target;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(VisibilityTransition, Source, Target);
            }

            public static bool operator ==(Params lhs, Params rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(Params lhs, Params rhs)
            {
                return !(lhs == rhs);
            }

            public AnimationPlayable CreateTargetPlayable(float startTime, bool reverse)
            {
                return CreatePlayable(Target, WidgetVisibility.Visible, VisibilityTransition.EntryAnimation, startTime, VisibilityTransition.Length, 
                    VisibilityTransition.EasingMode, reverse);
            }

            public AnimationPlayable CreateSourcePlayable(float startTime, bool reverse)
            {
                return CreatePlayable(Source, WidgetVisibility.Hidden, VisibilityTransition.ExitAnimation, startTime, VisibilityTransition.Length, 
                    VisibilityTransition.EasingMode.GetInverseEasingMode(), reverse);
            }

            private AnimationPlayable CreatePlayable(IWidget window, WidgetVisibility visibility, ImplicitAnimation implicitAnimation, float startTime, 
                float length, EasingMode easingMode, bool reverse)
            {
                if(!implicitAnimation.IsValid) throw new InvalidOperationException("Invalid implicit animation.");
                IAnimation widgetAnimation = implicitAnimation.GetAnimation(window, visibility);
                EasingMode easing = reverse ? easingMode.GetInverseEasingMode() : easingMode;
                return widgetAnimation.Playable(length, startTime, PlaybackMode.Forward, easing, _timeMode);
            }
        }

        private class QueuedTransition
        {
            public Params TransitionParams => _transitionParams;
            private Params _transitionParams;
            public bool Reverse { get; private set; } = false;

            private QueuedTransition() { }

            public QueuedTransition(in Params transitionParams, bool reverse)
            {
                _transitionParams = transitionParams;
                this.Reverse = reverse;
            }

            public bool ValuesAreEqualTo(QueuedTransition other)
            {
                return _transitionParams == other._transitionParams && Reverse == other.Reverse;
            }

            public bool ValuesAreEqualTo(in Params otherParams, bool otherReverse)
            {
                return _transitionParams == otherParams && Reverse == otherReverse;
            }

            public ref Params GetTransitionParamsRef()
            {
                return ref _transitionParams;
            }
        }

        public bool IsTransitionActive { get; private set; } = false;
        private Params _activeTransitionParams = new Params();
        private bool _isActiveReverse = false;

        private TimeMode _timeMode = TimeMode.Scaled;

        private int _queuedTransitionsCount { get { return _queuedTransitions.Count; } }
        private List<QueuedTransition> _queuedTransitions = new List<QueuedTransition>();

        private AnimationPlayer.PlaybackData _sourceWindowPlaybackData = default;
        private AnimationPlayer.PlaybackData _targetWindowPlaybackData = default;

        private AnimationPlayer.PlaybackData _primaryPlaybackData
        {
            get
            {
                switch (_activeTransitionParams.VisibilityTransition.Target)
                {
                    case TransitionTarget.Both:
                    case TransitionTarget.Target:
                        return _sourceWindowPlaybackData;
                    case TransitionTarget.Source:
                        return _targetWindowPlaybackData;
                }
                return default;
            }
        }

        private bool _terminating = false;

        private TransitionManager() { }

        public TransitionManager(TimeMode timeMode)
        {
            _timeMode = timeMode;
        }

        public void Transition(in VisibilityTransitionParams visibilityTransition, IWindow sourceWindow, IWindow targetWindow)
        {
            Params transitionParams = new Params(visibilityTransition, sourceWindow, targetWindow, _timeMode);
            HandleNewTransition(in transitionParams, false);
        }

        public void ReverseTransition(in VisibilityTransitionParams visibilityTransition, IWindow sourceWindow, IWindow targetWindow)
        {
            Params transitionParams = new Params(visibilityTransition, sourceWindow, targetWindow, _timeMode);
            HandleNewTransition(in transitionParams, true);
        }

        private void HandleNewTransition(in Params transitionParams, bool reverse)
        {
            if (transitionParams.Source == null)
            {
                throw new ArgumentNullException(nameof(transitionParams.Source));
            }

            if (transitionParams.Target == null)
            {
                throw new ArgumentNullException(nameof(transitionParams.Target));
            }

            if (!IsTransitionActive)
            {
                ExecuteTransition(in transitionParams, reverse);
            }
            else
            {
                // bool to determin if the new transition reverses the leading transition
                // this is true is we attempt to reverse a transition that is running forward or
                // we attempt to run a forward transition while a matching transition is currently running reversed                
                bool newTransitionOpposesLeading;
                Params leadingTransitionParams;
                if (_queuedTransitionsCount > 0)
                {
                    // Don't actually need to check this as we ensure that with queued transitions the leading transition should match any that are queued.
                    //if (QueueContainsTransition(in transitionParams, reverse))
                    //{
                    //    throw new InvalidOperationException("Attempting to perform a transition that is already queued.");
                    //}

                    QueuedTransition leadingQueuedTransition = PeekQueuedTransition();
                    leadingTransitionParams = leadingQueuedTransition.TransitionParams;
                    newTransitionOpposesLeading = AreOpposingTransitions(in transitionParams, reverse, in leadingTransitionParams, leadingQueuedTransition.Reverse);
                }
                else
                {
                    if (_activeTransitionParams == transitionParams && _isActiveReverse == reverse)
                    {
                        throw new InvalidOperationException("Attempting to perform a transition that is already active.");
                    }

                    leadingTransitionParams = _activeTransitionParams;
                    newTransitionOpposesLeading = AreOpposingTransitions(in transitionParams, reverse, in leadingTransitionParams, _isActiveReverse);
                }

                // If newTransitionOpposesLeading is true we know that target and source screens match for both transitions as AreOpposingTransitions() checks
                // for the equality of both target and source screens as well as the inequality of reverse.
                if (newTransitionOpposesLeading)
                {
                    if (_queuedTransitionsCount > 0)
                    {
                        // If we are attempting to invert a queued transition we simply pop it from the queue as the operation becomes redundant.
                        _ = PopQueuedTransition();
                    }
                    else
                    {
                        InvertActiveTransition();
                    }
                }
                else
                {
                    if (reverse)
                    {
                        // Ensure that the enqueueing transition is viable given the current leading transition.
                        if (leadingTransitionParams.Source != transitionParams.Target)
                        {
                            throw new InvalidOperationException(
                                "Attempting to reverse a transition to a previous screen while the leading transitions source differs from the reverse transitions target.");
                        }

                        EnqueueTransition(in transitionParams, reverse);
                    }
                    else
                    {
                        // Ensure that the enqueueing transition is viable given the current leading transition.
                        if (leadingTransitionParams.Target != transitionParams.Source)
                        {
                            throw new InvalidOperationException(
                                "Attempting to transition to a new screen while the leading transitions target differs from the new transitions source.");
                        }

                        EnqueueTransition(in transitionParams, reverse);
                    }
                }
            }
        }

        private void ExecuteTransition(in Params transitionParams, bool reverse)
        {
            IsTransitionActive = true;
            _isActiveReverse = reverse;
            _activeTransitionParams = transitionParams;

            if (_activeTransitionParams.VisibilityTransition.SortPriority == VisibilityTransitionParams.SortPriority.Auto)
            {
                switch (_activeTransitionParams.VisibilityTransition.Target)
                {
                    default:
                    case VisibilityTransitionParams.AnimationTarget.Both:
                        _activeTransitionParams.Source.SortOrder = 0;
                        _activeTransitionParams.Target.SortOrder = 0;
                        break;
                    case VisibilityTransitionParams.AnimationTarget.Target:
                        _activeTransitionParams.Source.SortOrder = 0;
                        _activeTransitionParams.Target.SortOrder = 1;
                        break;
                    case VisibilityTransitionParams.AnimationTarget.Source:
                        _activeTransitionParams.Source.SortOrder = 1;
                        _activeTransitionParams.Target.SortOrder = 0;
                        break;
                }
            }
            else
            {
                switch (_activeTransitionParams.VisibilityTransition.SortPriority)
                {
                    case VisibilityTransitionParams.SortPriority.Source:
                        _activeTransitionParams.Source.SortOrder = 1;
                        _activeTransitionParams.Target.SortOrder = 0;
                        break;
                    case VisibilityTransitionParams.SortPriority.Target:
                        _activeTransitionParams.Source.SortOrder = 0;
                        _activeTransitionParams.Target.SortOrder = 1;
                        break;
                }
            }

            if (_activeTransitionParams.VisibilityTransition.Target == VisibilityTransitionParams.AnimationTarget.None)
            {
                ExecuteNone(in _activeTransitionParams, reverse);
                CompleteTransition();
            }
            else
            {
                ExecuteTransition(in _activeTransitionParams, _isActiveReverse, 0.0F);
            }
        }

        private void InvertActiveTransition()
        {
            if (_activeTransitionParams.VisibilityTransition.Target == VisibilityTransitionParams.AnimationTarget.None)
            {
                throw new InvalidOperationException("Cannot invert transition with no animation targets.");
            }

            _isActiveReverse = !_isActiveReverse;

            float normalisedStartOffset = _isActiveReverse ? 1.0F - _primaryPlaybackData.CurrentNormalisedTime : _primaryPlaybackData.CurrentNormalisedTime;
            ExecuteTransition(in _activeTransitionParams, _isActiveReverse, normalisedStartOffset);
        }

        private void ExecuteTransition(in Params transitionParams, bool reverse, float normalisedStartOffset)
        {
            float startOffset = transitionParams.VisibilityTransition.Length * normalisedStartOffset;
            switch (_activeTransitionParams.VisibilityTransition.Target)
            {
                case VisibilityTransitionParams.AnimationTarget.Both:
                    ExecuteOnBoth(in transitionParams, reverse, startOffset);
                    break;
                case VisibilityTransitionParams.AnimationTarget.Target:
                    ExecuteOnTarget(in transitionParams, reverse, startOffset);
                    break;
                case VisibilityTransitionParams.AnimationTarget.Source:
                    ExecuteOnSource(in transitionParams, reverse, startOffset);
                    break;
            }
        }

        private void ExecuteNone(in Params transitionParams, bool reverse)
        {
            if (reverse)
            {
                transitionParams.Source.Open();
                transitionParams.Target.Close();
            }
            else
            {
                transitionParams.Source.Close();
                transitionParams.Target.Open();
            }
        }

        private void ExecuteOnTarget(in Params transitionParams, bool reverse, float startOffset)
        {
            if (reverse)
            {
                transitionParams.Source.Open();

                AccessAnimationPlayable targetWindowPlayable = transitionParams.CreateTargetPlayable(startOffset, reverse);
                transitionParams.Target.Close(in targetWindowPlayable, OnAccessAnimationComplete);
            }
            else
            {
                AccessAnimationPlayable targetWindowPlayable = transitionParams.CreateTargetPlayable(startOffset, reverse);
                transitionParams.Target.Open(in targetWindowPlayable, OnAccessAnimationComplete);
            }
            _targetWindowPlaybackData = transitionParams.Target.AccessAnimationPlaybackData;
        }

        private void ExecuteOnSource(in Params transitionParams, bool reverse, float startOffset)
        {
            if (reverse)
            {
                AccessAnimationPlayable sourceWindowPlayable = transitionParams.CreateSourcePlayable(startOffset, reverse);
                transitionParams.Source.Open(in sourceWindowPlayable, OnAccessAnimationComplete);
            }
            else
            {
                transitionParams.Target.Open();

                AccessAnimationPlayable sourceWindowPlayable = transitionParams.CreateSourcePlayable(startOffset, reverse);
                transitionParams.Source.Close(in sourceWindowPlayable, OnAccessAnimationComplete);
            }
            _sourceWindowPlaybackData = transitionParams.Source.AccessAnimationPlaybackData;
        }

        private void ExecuteOnBoth(in Params transitionParams, bool reverse, float startOffset)
        {
            if (reverse)
            {
                AccessAnimationPlayable sourceWindowPlayable = transitionParams.CreateSourcePlayable(startOffset, reverse);
                transitionParams.Source.Open(in sourceWindowPlayable, OnAccessAnimationComplete);

                AccessAnimationPlayable targetWindowPlayable = transitionParams.CreateTargetPlayable(startOffset, reverse);
                transitionParams.Target.Close(in targetWindowPlayable, OnAccessAnimationComplete);
            }
            else
            {
                AccessAnimationPlayable sourceWindowPlayable = transitionParams.CreateSourcePlayable(startOffset, reverse);
                transitionParams.Source.Close(in sourceWindowPlayable, OnAccessAnimationComplete);

                AccessAnimationPlayable targetWindowPlayable = transitionParams.CreateTargetPlayable(startOffset, reverse);
                transitionParams.Target.Open(in targetWindowPlayable, OnAccessAnimationComplete);
            }
            _targetWindowPlaybackData = transitionParams.Target.AccessAnimationPlaybackData;
            _sourceWindowPlaybackData = transitionParams.Source.AccessAnimationPlaybackData;
        }

        private void EnqueueTransition(in Params transitionParams, bool reverse)
        {
            QueuedTransition queuedTransition = new QueuedTransition(in transitionParams, reverse);
            _queuedTransitions.Add(queuedTransition);
        }

        private QueuedTransition DequeueTransition()
        {
            if (_queuedTransitionsCount == 0)
            {
                throw new InvalidOperationException("The transition queue is empty.");
            }
            QueuedTransition dequeuedTransition = _queuedTransitions[0];
            _queuedTransitions.RemoveAt(0);
            return dequeuedTransition;
        }

        private QueuedTransition PopQueuedTransition()
        {
            if (_queuedTransitionsCount == 0)
            {
                throw new InvalidOperationException("The transition queue is empty.");
            }
            int popIndex = _queuedTransitionsCount - 1;
            QueuedTransition poppedTransition = _queuedTransitions[popIndex];
            _queuedTransitions.RemoveAt(popIndex);
            return poppedTransition;
        }

        private QueuedTransition PeekQueuedTransition()
        {
            if (_queuedTransitionsCount == 0)
            {
                throw new InvalidOperationException("The transition queue is empty.");
            }
            return _queuedTransitions[_queuedTransitionsCount - 1];
        }

        private bool QueueContainsTransition(in Params transitionParams, bool reverse)
        {
            for (int i = _queuedTransitionsCount - 1; i >= 0; i--)
            {
                if (_queuedTransitions[i].TransitionParams == transitionParams && _queuedTransitions[i].Reverse == reverse)
                {
                    return true;
                }
            }
            return false;
        }

        private void CompleteTransition()
        {
            if (_activeTransitionParams.VisibilityTransition.Target != VisibilityTransitionParams.AnimationTarget.Both)
            {
                if (_isActiveReverse)
                {
                    if (_activeTransitionParams.VisibilityTransition.Target == VisibilityTransitionParams.AnimationTarget.Source)
                    {
                        _activeTransitionParams.Target.Close();
                    }
                }
                else
                {
                    if (_activeTransitionParams.VisibilityTransition.Target == VisibilityTransitionParams.AnimationTarget.Target)
                    {
                        _activeTransitionParams.Source.Close();
                    }
                }
            }

            if (_queuedTransitionsCount > 0)
            {
                QueuedTransition queuedTransition = DequeueTransition();
                ExecuteTransition(in queuedTransition.GetTransitionParamsRef(), queuedTransition.Reverse);
            }
            else
            {
                IsTransitionActive = false;
                _isActiveReverse = false;
                _activeTransitionParams.ReleaseReferences();
            }
        }

        private void OnAccessAnimationComplete(IAccessible accessible)
        {
            if (!_terminating)
            {
                if (accessible == _activeTransitionParams.Source)
                {
                    _sourceWindowPlaybackData.ReleaseReferences();
                }

                if (accessible == _activeTransitionParams.Target)
                {
                    _targetWindowPlaybackData.ReleaseReferences();
                }

                if (!_sourceWindowPlaybackData.IsPlaying && !_targetWindowPlaybackData.IsPlaying)
                {
                    CompleteTransition();
                }
            }
        }

        private bool AreOpposingTransitions(in Params lhsTransitionParams, bool lhsReverse, in Params rhsTransitionParams, bool rhsReverse)
        {
            return lhsTransitionParams.Target == rhsTransitionParams.Target && lhsTransitionParams.Source == rhsTransitionParams.Source &&
                lhsReverse != rhsReverse;
        }

        public void Terminate(bool skipAnimations)
        {
            _terminating = true;
            if (IsTransitionActive)
            {
                _targetWindowPlaybackData.ReleaseReferences();
                _sourceWindowPlaybackData.ReleaseReferences();

                if (skipAnimations)
                {
                    switch (_activeTransitionParams.VisibilityTransition.Target)
                    {
                        case VisibilityTransitionParams.AnimationTarget.Source:
                            _activeTransitionParams.Source.SkipAccessAnimation();
                            if (_isActiveReverse)
                            {
                                _activeTransitionParams.Target.Close();
                            }
                            break;
                        case VisibilityTransitionParams.AnimationTarget.Target:
                            _activeTransitionParams.Target.SkipAccessAnimation();
                            if (!_isActiveReverse)
                            {
                                _activeTransitionParams.Source.Close();
                            }
                            break;
                        case VisibilityTransitionParams.AnimationTarget.Both:
                            _activeTransitionParams.Source.SkipAccessAnimation();
                            _activeTransitionParams.Target.SkipAccessAnimation();
                            break;
                    }
                }

                IsTransitionActive = false;
                _isActiveReverse = false;
                _activeTransitionParams.ReleaseReferences();
                _queuedTransitions.Clear();
            }
            _terminating = false;
        }
    }
}