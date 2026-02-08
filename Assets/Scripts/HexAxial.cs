// HexGrid.cs (new)
using System;
using UnityEngine;

public readonly struct HexAxial : IEquatable<HexAxial>
{
    public readonly int q;
    public readonly int r;

    public HexAxial(int q, int r) { this.q = q; this.r = r; }

    public bool Equals(HexAxial other) => q == other.q && r == other.r;
    public override bool Equals(object obj) => obj is HexAxial h && Equals(h);
    public override int GetHashCode() => (q * 397) ^ r;
    public override string ToString() => $"({q},{r})";

    public static bool operator ==(HexAxial a, HexAxial b) => a.Equals(b);
    public static bool operator !=(HexAxial a, HexAxial b) => !a.Equals(b);
}

public static class HexGrid
{
    // Pointy-top axial layout.
    // sizeUnits = distance from center to a corner in map-space units.
    public static Vector2 AxialToLocalXY(HexAxial h, float sizeUnits)
    {
        float x = sizeUnits * Mathf.Sqrt(3f) * (h.q + h.r * 0.5f);
        float y = sizeUnits * 1.5f * h.r;
        return new Vector2(x, y);
    }

    public static HexAxial LocalXYToAxial(Vector2 local, float sizeUnits)
    {
        // Inverse of the above (fractional axial)
        float q = (Mathf.Sqrt(3f) / 3f * local.x - 1f / 3f * local.y) / sizeUnits;
        float r = (2f / 3f * local.y) / sizeUnits;

        return CubeRound(q, r);
    }

    // Hex distance in axial coords
    public static int Distance(HexAxial a, HexAxial b)
    {
        int aq = a.q, ar = a.r;
        int bq = b.q, br = b.r;

        int ax = aq;
        int az = ar;
        int ay = -ax - az;

        int bx = bq;
        int bz = br;
        int by = -bx - bz;

        return (Mathf.Abs(ax - bx) + Mathf.Abs(ay - by) + Mathf.Abs(az - bz)) / 2;
    }

    private static HexAxial CubeRound(float q, float r)
    {
        // Convert axial to cube, round, then back.
        float x = q;
        float z = r;
        float y = -x - z;

        int rx = Mathf.RoundToInt(x);
        int ry = Mathf.RoundToInt(y);
        int rz = Mathf.RoundToInt(z);

        float dx = Mathf.Abs(rx - x);
        float dy = Mathf.Abs(ry - y);
        float dz = Mathf.Abs(rz - z);

        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;

        return new HexAxial(rx, rz);
    }

    // neighbor center-to-center distance = sizeUnits * sqrt(3)
    public static float NeighborDistanceUnits(float sizeUnits) => sizeUnits * Mathf.Sqrt(3f);
}
