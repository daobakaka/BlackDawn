using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景启动接口
/// </summary>
public interface IECSSyncedMono
{
    void OnSceneEcsReady();
    bool Enable { get; set; }
}
/// <summary>
/// 单次运行接口
/// </summary>
public interface IOneStepFun
{ 
  bool Done { get; set; }
}
/// <summary>
/// entity 预制体对接口，用于泛型
/// </summary>
/// <typeparam name="TEnum"></typeparam>
public interface IPrefabPair<TEnum> where TEnum : struct, Enum
{
    TEnum ID { get; }
    GameObject Prefab { get; }
}
