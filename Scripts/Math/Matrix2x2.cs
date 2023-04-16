using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct Matrix2x2
{
    float m00, m01, m10, m11; //m{x}{y} with x row and y column

    public static Vector2 operator *(Matrix2x2 rhs, Vector2 lhs)
    {
        return new Vector2(rhs.m00 * lhs.x + rhs.m01 * lhs.y, rhs.m10 * lhs.x + rhs.m11 * lhs.y);
    }

    public static Matrix2x2 operator *(float rhs, Matrix2x2 lhs)
    {
        lhs.m00 *= rhs;
        lhs.m01 *= rhs;
        lhs.m10 *= rhs;
        lhs.m11 *= rhs;
        return lhs;
    }

    public Matrix2x2(Vector2 column0, Vector2 column1)
    {
        m00 = column0.x;
        m10 = column0.y;

        m01 = column1.x;
        m11 = column1.y;
    }

    public Matrix2x2 inverse
    {
        get
        {
            float invDet = 1f / (m00 * m11 - m01 * m10);

            Matrix2x2 mat = new Matrix2x2();

            mat.m00 = m11 * invDet;
            mat.m01 = -m01 * invDet;
            mat.m10 = -m10 * invDet;
            mat.m11 = m00 * invDet;

            return mat;
        }
    }

    public float determinant
    {
        get
        {
            return m00 * m11 - m01 * m10;
        }
    }

}
