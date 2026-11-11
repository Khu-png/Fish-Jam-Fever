using System.Collections.Generic;
using UnityEngine;

namespace FishSort
{
    // Finds a downhill route for a bubble across the other bubbles (circles). It only plans: the bubble is
    // moved by Physics2D (gravity) plus a small bounded sideways push, so everything else stays physical.
    // A circle is a Vector3: x, y = centre, z = collider radius.
    public static class BubbleSettlePlanner
    {
        private const int STEPS_PER_SIDE = 14;
        private const float HEIGHT_TOLERANCE = 0.025f;
        private const float OVERLAP_TOLERANCE = 0.03f;
        private const float TRAVEL_COST = 0.08f;
        private const float SAME_SCORE_TOLERANCE = 0.005f;
        private const float DIRECTION_MEMORY_DISTANCE = 0.05f;
        private const float CEILING_RADIUS_RATIO = 0.05f;
        private const float DESIRED_SPEED_GAIN = 2f;
        private const float ACCELERATION_GAIN = 4f;
        // Above this downward speed the bubble is in free fall and the sideways push fades out completely.
        private const float FREE_FALL_SPEED = 3f;

        public static Vector2 FindTarget(IReadOnlyList<Vector3> circles, int index, float floorY,
            float halfWidth, float searchDistance, float minDrop, float previousTargetX)
        {
            Vector3 source = circles[index];
            float left = -halfWidth + source.z;
            float right = halfWidth - source.z;
            Vector2 best = new Vector2(source.x, source.y);
            float bestScore = Mathf.Min(source.y, SupportHeight(circles, index, source.x, floorY));
            int firstDirection = FirstDirection(previousTargetX - source.x, index);
            for (int side = 0; side < 2; side++)
            {
                int direction = side == 0 ? firstDirection : -firstDirection;
                bool isBlocked = false;
                for (int step = 1; step <= STEPS_PER_SIDE && !isBlocked; step++)
                {
                    float x = Mathf.Clamp(source.x + direction * searchDistance * step / STEPS_PER_SIDE, left, right);
                    float y = SupportHeight(circles, index, x, floorY);
                    // Never pull a bubble uphill, and never through a neighbour standing in its way.
                    isBlocked = y > source.y + HEIGHT_TOLERANCE || Overlaps(circles, index, x, source.y, source.z);
                    float score = y + Mathf.Abs(x - source.x) * TRAVEL_COST;
                    bool isRealPocket = source.y - y >= minDrop;
                    if (!isBlocked && isRealPocket && score < bestScore - SAME_SCORE_TOLERANCE)
                    {
                        bestScore = score;
                        best = new Vector2(x, y);
                    }
                }
            }

            return source.y - best.y >= minDrop ? best : new Vector2(source.x, source.y);
        }

        // Sideways acceleration that steers towards the target without ever fighting gravity.
        public static float SteeringAcceleration(float displacement, float horizontalSpeed,
            float downwardSpeed, float maxSpeed, float maxAcceleration)
        {
            float desiredSpeed = Mathf.Clamp(displacement * DESIRED_SPEED_GAIN, -maxSpeed, maxSpeed);
            float acceleration = Mathf.Clamp((desiredSpeed - horizontalSpeed) * ACCELERATION_GAIN, -maxAcceleration, maxAcceleration);
            return acceleration * (1f - Mathf.Clamp01(Mathf.Abs(downwardSpeed) / FREE_FALL_SPEED));
        }

        // Keep going the way the bubble was already heading; otherwise alternate by index so neighbours differ.
        private static int FirstDirection(float previousOffset, int index)
        {
            int direction = index % 2 == 0 ? -1 : 1;
            if (Mathf.Abs(previousOffset) > DIRECTION_MEMORY_DISTANCE)
            {
                direction = previousOffset < 0f ? -1 : 1;
            }

            return direction;
        }

        // Centre height at which a circle at x rests on the circles below it (or on the floor).
        private static float SupportHeight(IReadOnlyList<Vector3> circles, int index, float x, float floorY)
        {
            Vector3 source = circles[index];
            float height = floorY + source.z;
            for (int i = 0; i < circles.Count; i++)
            {
                Vector3 other = circles[i];
                // Bubbles at or above this one are ceilings, not supports.
                bool isSupport = i != index && other.y < source.y - source.z * CEILING_RADIUS_RATIO;
                float reach = source.z + other.z;
                float dx = x - other.x;
                if (isSupport && Mathf.Abs(dx) < reach)
                {
                    height = Mathf.Max(height, other.y + Mathf.Sqrt(reach * reach - dx * dx));
                }
            }

            return height;
        }

        // True when a circle of this radius at (x, y) would sink into another bubble.
        private static bool Overlaps(IReadOnlyList<Vector3> circles, int index, float x, float y, float radius)
        {
            bool overlaps = false;
            for (int i = 0; i < circles.Count && !overlaps; i++)
            {
                if (i != index)
                {
                    float reach = radius + circles[i].z - OVERLAP_TOLERANCE;
                    float dx = x - circles[i].x;
                    float dy = y - circles[i].y;
                    overlaps = dx * dx + dy * dy < reach * reach;
                }
            }

            return overlaps;
        }
    }
}
