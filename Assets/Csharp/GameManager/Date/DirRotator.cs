using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static InitCubeSlot;

public static class DirRotator
{
    //旋转映射表
        //绕Y顺时针（都默认看向负方向）
    static FaceDir RotateY_CW(FaceDir d)
    {
        return d switch
        {
            FaceDir.Front => FaceDir.Right,
            FaceDir.Left => FaceDir.Front,
            FaceDir.Back => FaceDir.Left,
            FaceDir.Right => FaceDir.Back,
            _ => d
        };
    }

        //绕Y逆时针
    static FaceDir RotateY_CCW(FaceDir d)
    {
        return d switch
        {
            FaceDir.Front => FaceDir.Left,
            FaceDir.Left => FaceDir.Back,
            FaceDir.Back => FaceDir.Right,
            FaceDir.Right => FaceDir.Front,
            _ => d
        };
    }
    //绕X顺时针
    static FaceDir RotateX_CW(FaceDir d)
    {
        return d switch
        {
            FaceDir.Up => FaceDir.Front,
            FaceDir.Back => FaceDir.Up,
            FaceDir.Down => FaceDir.Back,
            FaceDir.Front => FaceDir.Down,
            _ => d
        };
    }

    //绕X逆时针
    static FaceDir RotateX_CCW(FaceDir d)
    {
        return d switch
        {
            FaceDir.Up => FaceDir.Back,
            FaceDir.Back => FaceDir.Down,
            FaceDir.Down => FaceDir.Front,
            FaceDir.Front => FaceDir.Up,
            _ => d
        };
    }
    //绕Z轴顺时针
    static FaceDir RotateZ_CW(FaceDir d)
    {
        return d switch
        {
            FaceDir.Up => FaceDir.Left,
            FaceDir.Right => FaceDir.Up,
            FaceDir.Down => FaceDir.Right,
            FaceDir.Left => FaceDir.Down,
            _ => d
        };
    }

    //绕Z轴逆时针
    static FaceDir RotateZ_CCW(FaceDir d)
    {
        return d switch
        {
            FaceDir.Up => FaceDir.Right,
            FaceDir.Right => FaceDir.Down,
            FaceDir.Down => FaceDir.Left,
            FaceDir.Left => FaceDir.Up,
            _ => d
        };
    }

    //更新面朝向的方法
    public static FaceDir Rotate(FaceDir d, Axis axis, bool cw)
    {
        return axis switch
        {
            Axis.X => cw ? RotateX_CW(d) : RotateX_CCW(d),
            Axis.Y => cw ? RotateY_CW(d) : RotateY_CCW(d),
            Axis.Z => cw ? RotateZ_CW(d) : RotateZ_CCW(d),
            _ => d
        };
    }
}