using UnityEngine;

public class CustomRenderLogic : MonoBehaviour
{
    public MeshRenderer[] meshRenderer;
    public GameObject[] objects;
    public bool setDissovle;
    public bool setUnderAattack;
    public bool setFreeze;
    public bool setFire;
    public bool setPoisoning;
    public bool setLighting;
    public bool setDarkShadow;
    private float _timer;
    private MaterialPropertyBlock _mpbUnderAttack;
    private MaterialPropertyBlock _mpbDissolve;
    private MaterialPropertyBlock _mpbFreeze;
    private MaterialPropertyBlock _mpbFire;
    //�ж�,���˪���÷�����ֵ�����л�
    private MaterialPropertyBlock _mpbPoisoning;
    //���磬�밵Ӱ�����棬�ܽ⹲��Emissionͨ��
    private MaterialPropertyBlock _mpbLighting;
    private MaterialPropertyBlock _mpbDarkShadow;
    void Start()
    {
        // Ϊÿ�������ȡ MeshRenderer
        for (int i = 0; i < objects.Length; i++)
        {
            meshRenderer[i] = objects[i].GetComponent<MeshRenderer>();
        }
        // �ֱ𴴽����������� MPB
        _mpbUnderAttack = new MaterialPropertyBlock();
        _mpbDissolve = new MaterialPropertyBlock();
        _mpbFreeze = new MaterialPropertyBlock();
        _mpbFire = new MaterialPropertyBlock();
        _mpbPoisoning = new MaterialPropertyBlock();
        _mpbLighting = new MaterialPropertyBlock();
        _mpbDarkShadow = new MaterialPropertyBlock();
    }

    void Update()
    {
        float colorPar = Mathf.PingPong(Time.time, 1f);

        if (setUnderAattack)
        {
            _mpbUnderAttack.SetColor("_UnderAttackColor", new Color(colorPar, colorPar, colorPar));
            _mpbUnderAttack.SetFloat("_FireIntensity", 0);
            meshRenderer[0].SetPropertyBlock(_mpbUnderAttack);
        }
        //ͬһ����ڵ�Ĳ��ʸ������⣬����shader���̽��и��ǣ����������ȷ�����˳���ܽ⡢���������桢���ء����硢��Ӱ
        if (setDissovle)
        {
            _mpbDissolve.SetFloat("_DissolveEffect", colorPar);
            _mpbDissolve.SetFloat("_FreezeAmount", 0);
            meshRenderer[1].SetPropertyBlock(_mpbDissolve);
        }

        if (setFreeze)
        {
            _mpbFreeze.SetFloat("_DissolveEffect", 0);
            _mpbFreeze.SetFloat("_FreezeAmount", colorPar);
            _mpbFreeze.SetColor("_FresnelColor", new Color(0, 1, 1, 1));
            meshRenderer[2].SetPropertyBlock(_mpbFreeze);
        }
        if (setFire)
        {
            _mpbFire.SetColor("_UnderAttackColor", new Color(0, 0, 0));
            _mpbFire.SetFloat("_FireIntensity", colorPar);
            _mpbFire.SetFloat("_LightingIntensity", 0);
            meshRenderer[3].SetPropertyBlock(_mpbFire);
            if(colorPar>=0.99)
            objects[3].transform.GetChild(0).gameObject.SetActive(true);
        }
        if (setPoisoning)
        {
          
            _mpbPoisoning.SetFloat("_DissolveEffect", 0);
            _mpbPoisoning.SetFloat("_PoisoningIntensity", colorPar);
            _mpbPoisoning.SetColor("_FresnelColor", new Color(0.1f, 1, 0, 1));
            meshRenderer[4].SetPropertyBlock(_mpbPoisoning);
        }

        if (setLighting)
        {
            _mpbLighting.SetColor("_UnderAttackColor", new Color(0, 0, 0));
            _mpbLighting.SetFloat("_FireIntensity", 0);
            _mpbLighting.SetFloat("_LightingIntensity", colorPar);
            _mpbLighting.SetColor("_FresnelColor", new Color(0.8f, 1, 1, 1));
            meshRenderer[5].SetPropertyBlock(_mpbLighting);
        }

        if (setDarkShadow)
        {
            _mpbDarkShadow.SetColor("_UnderAttackColor", new Color(0, 0, 0));
            _mpbDarkShadow.SetFloat("_FireIntensity", 0);
            _mpbDarkShadow.SetFloat("_LightingIntensity", 0);
            _mpbDarkShadow.SetFloat("_DarkShadowIntensity", colorPar);
            _mpbDarkShadow.SetColor("_FresnelColor", new Color(1, 0, 1, 1));
            meshRenderer[6].SetPropertyBlock(_mpbDarkShadow);

        }

    }
}
