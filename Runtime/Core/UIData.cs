using System;

namespace UIFramework.Core
{
    public class UIDataException : Exception
    {
        public UIDataException() { }

        public UIDataException(string message)
            : base(message) { }

        public UIDataException(string message, Exception inner)
            : base(message, inner) { }
    }

    public static class UIData
    {
        public static bool IsValid<ObjectType>(object inputObject, bool allowNull = false)
        {
            System.Type expectedType = typeof(ObjectType);
            if (inputObject != null)
            {
                System.Type inType = inputObject.GetType();
                if (inType == expectedType || inType.IsSubclassOf(expectedType))
                {
                    return true;
                }
            }
            else if (allowNull && expectedType.IsSubclassOf(typeof(object)))
            {
                return true;
            }
            return false;
        }

        public static ObjectType ValidateObject<ObjectType>(object inputObject, bool allowNull = false)
        {
            ObjectType output;
            if (!ValidateObjectType(inputObject, allowNull, out output))
            {
                string type = inputObject == null ? "null" : inputObject.GetType().ToString();
                throw new UIDataException(string.Format("Unexpected type, object is: {0}, expected object: {1}", type, typeof(ObjectType)));
            }
            return output;
        }

        private static bool ValidateObjectType<ObjectType>(object inObject, bool allowNull, out ObjectType outObject)
        {
            outObject = default;
            System.Type expectedType = typeof(ObjectType);
            if (inObject != null)
            {
                System.Type inType = inObject.GetType();
                if (inType == expectedType || inType.IsSubclassOf(expectedType))
                {
                    outObject = (ObjectType)inObject;
                    return true;
                }
            }
            else if (allowNull && expectedType.IsSubclassOf(typeof(object)))
            {
                return true;
            }
            return false;
        }
    }
}
