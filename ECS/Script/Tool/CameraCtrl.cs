using BlackDawn;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCtrl : MonoBehaviour //相机控制
{
    public static CameraCtrl instance;
    [Header("场景Mono")] public GameObject gameMono;
    [Header("相机偏移量")] public Vector3 cameraOffset;  // 相机相对于角色的偏移量
    [SerializeField][Header("相机跟随速度")] private float cameraFollowSpeed;  // 控制相机跟随的速度



    void Awake()
    {
        instance = this;

    }

    private CameraCtrl() { }
    // Start is called before the first frame update
    void Start()
    {
        
      
        
        //制造敌人
        //InstanEnemy();
        //playerRole = RoleManager.instance.Cur_Role.gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        if(gameMono == null) return;
        if (gameMono.transform.childCount==0) return;
        else
        {
            // 相机平滑跟随角色
            transform.position = Vector3.Lerp(transform.position, gameMono.transform.GetChild(0).position + cameraOffset, cameraFollowSpeed * Time.deltaTime);
            cameraOffset = Hero.instance.cameraDistance;
        }

    }

    //获取鼠标的世界坐标
    public Vector3 GetMousePos()
    {
        LayerMask groundLayer = LayerMask.GetMask("Map_Ground");
        // 获取鼠标位置，转换为射线
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        // 用射线检测地面
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            return hit.point; // 返回碰撞点作为鼠标在世界中的位置
        }

        return Vector3.zero; // 如果没有击中地面，返回一个无效位置
    }


}
