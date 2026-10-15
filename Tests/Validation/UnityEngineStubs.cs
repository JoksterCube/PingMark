namespace JoksterCube.PingMark.Validation.UnityEngine;

public struct Vector3
{
    public float x, y, z;
    public Vector3(float distance) { x = distance; y = z = 0; }
    public float sqrMagnitude => x * x + y * y + z * z;
    public static Vector3 operator -(Vector3 left, Vector3 right) => new Vector3(left.x - right.x);
}
public class Transform { public Vector3 position; }