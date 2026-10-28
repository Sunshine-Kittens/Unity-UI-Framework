// Optimized rounded box distance function
float GetRoundedBoxDistance(float2 pos, float2 center, float radius, float inset)
{
	// Translate to quadrant space
	pos = abs(pos - center) - (center - (radius + inset));

	// Distance to corner arc
	float cornerDist = length(pos) - radius;

	// Distance to straight edge
	float edgeDist = max(pos.x - radius, pos.y - radius);

	// Select edge distance if outside quadrant, else corner distance
	return (pos.x <= 0 || pos.y <= 0) ? edgeDist : cornerDist;
}

// Optimized rounded box element color function
float4 GetRoundedBoxElementColor(
	float2 size, float2 texcoord, float4 cornerRadii,
	half thickness, half4 color, half4 borderColor)
{
	float2 pos = size * texcoord;
	float2 center = size * 0.5;

	// Determine quadrant (0 or 1)
	float2 quadrant = step(texcoord, float2(0.5, 0.5));

	// Select radius based on quadrant
	float left   = quadrant.x ? cornerRadii.x : cornerRadii.y;
	float right  = quadrant.x ? cornerRadii.w : cornerRadii.z;
	float radius = quadrant.y ? left : right;

	// Distances for outer and inner borders
	float dext = GetRoundedBoxDistance(pos, center, radius, 0.0);
	float din  = GetRoundedBoxDistance(pos, center, max(radius - thickness, 0), thickness);

	// Smooth transitions (spread inlined)
	float bi = smoothstep(0.5, -0.5, dext);
	float fi = smoothstep(0.5, -0.5, din);

	// Base color choice (avoid lerp with bool → use conditional)
	float4 OutColor = (thickness > radius) ? color : borderColor;
	OutColor.a = 0.0;

	// Blend border and fill
	OutColor = lerp(OutColor, borderColor, bi);
	OutColor = lerp(OutColor, color, fi);

	return OutColor;
}