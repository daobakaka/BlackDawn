using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���������ӿ�
/// </summary>
public interface IECSSyncedMono
{
    void OnSceneEcsReady();
    bool Enable { get; set; }
}
/// <summary>
/// �������нӿ�
/// </summary>
public interface IOneStepFun
{ 
  bool Done { get; set; }
}
/// <summary>
/// entity Ԥ����Խӿڣ����ڷ���
/// </summary>
/// <typeparam name="TEnum"></typeparam>
public interface IPrefabPair<TEnum> where TEnum : struct, Enum
{
    TEnum ID { get; }
    GameObject Prefab { get; }
}
