
//灵能的一些设计
namespace BlackDawn.DOTS
{
    /// <summary>
    /// 全量灵能 ID 枚举
    /// </summary>
    public enum PsionicsID
    {
        // 狂暴威能
        KuangBaoWeiNeng,
        // 元素共鸣
        YuanSuGongMing,
        // 急速威能
        JiSuWeiNeng,
        // 扩散威能
        KuoSanWeiNeng,
        // 吸血威能
        XiXueWeiNeng,
        // 穿透威能
        ChuanTouWeiNeng,
        // 余震威能
        YuZhenWeiNeng,
        // 连锁威能
        LianSuoWeiNeng,
        // 蓄力威能
        XuLiWeiNeng,
        // 过载威能
        GuoZaiWeiNeng,
        // 能力萃取
        NengLiCuiQu,
        // 黄金律
        HuangJinLv,
        // 伤害转化
        ShangHaiZhuanHua,
        // 节能专家
        JieNengZhuanJia,
        // 彩虹轨迹
        CaiHongGuiJi,
        // 肾上腺素
        ShenShangXianSu,
        // 杀戮节奏
        ShaLuJieZou,
        // 复仇者
        FuChouZhe,
        // 能量循环
        NengLiangXunHuan,
        // 弹药专家
        DanYaoZhuanJia,
        // 元素亲和
        YuanSuQinHe,
        // 元素护盾
        YuanSuHuDun,
        // 元素扩散
        YuanSuKuoSan,
        // 冷却加速
        LengQueJiaSu,
        // 脉冲震荡
        MaiChongZhenDang,
        // 脉冲分裂
        MaiChongFenLie,
        // 暗能充能
        AnNengChongNeng,
        // 暗能吞噬
        AnNengTunShi,
        // 冰火风暴
        BingHuoFengBao,
        // 元素融合
        YuanSuRongHe,
        // 雷暴增幅
        LeiBaoZengFu,
        // 静电附着
        JingDianFuZhuo,
        // 生命虹吸
        ShengMingHongXi,
        // 神圣净化
        ShenShengJingHua,
        // 极寒穿透
        JiHanChuanTou,
        // 冰霜新星
        BingShuangXinXing,
        // 暗影连闪
        AnYingLianShan,
        // 暗影爆破
        AnYingBaoPo,
        // 雷暴牢笼
        LeiBaoLaoLong,
        // 导电牢笼
        DaoDianLaoLong,
        // 瘟疫地雷
        WenYiDiLei,
        // 麻痹地雷
        MaBiDiLei,
        // 洪流聚焦
        HongLiuJuJiao,
        // 暗影余波
        AnYingYuBo,
        // 时间凝滞
        ShiJianNingZhi,
        // 加速领域
        JiaSuLingYu,
        // 烈焰爆冲
        LieYanBaoChong,
        // 灼热路径
        ZhuoReLuJing,
        // 极寒反噬
        JiHanFanShi,
        // 永冻护盾
        YongDongHuDun,
        // 死亡串联
        SiWangChuanLian,
        // 暗影饥渴
        AnYingJiKe,
        // 雷神之握
        LeiShenZhiWo,
        // 过载抓取
        GuoZaiZhuaQu,
        // 爆燃印记
        BaoRanYinJi,
        // 烈焰传染
        LieYanChuanRan,
        // 极寒新星
        JiHanXinXing,
        // 碎冰冲击
        SuiBingChongJi,
        // 暗影大师
        AnYingDaShi,
        // 致命突袭
        ZhiMingTuXi,
        // 超级传播
        ChaoJiChuanBo,
        // 剧毒变异
        JuDuBianYi,
        // 元素过载
        YuanSuGuoZai,
        // 元素调和
        YuanSuTiaoHe,
        // 奥术过载
        AoShuGuoZai,
        // 魔力瓦解
        MoLiWaJie,
        // 时空回溯
        ShiKongHuiSu,
        // 幻象大师
        HuanXiangDaShi,
        // 爆燃冲击
        BaoRanChongJi,
        // 灼热核心
        ZhuoReHeXin,
        // 极寒之路
        JiHanZhiLu,
        // 冰晶之径
        BingJingZhiJing,
        // 超导链接
        ChaoDaoLianJie,
        // 雷霆审判
        LeiTingShenPan,
        // 暗影收割
        AnYingShouGe,
        // 影袭连击
        YingXiLianJi,
        // 麻痹毒雾
        MaBiDuWu,
        // 腐蚀毒云
        FuShiDuYun,
        // 元素聚焦
        YuanSuJuJiao,
        // 混沌爆发
        HunDunBaoFa,
        // 幻影大师
        HuanYingDaShi,
        // 闪避专家
        ShanBiZhuanJia,
        // 影武者
        YingWuZhe,
        // 暗影同步
        AnYingTongBu,
        // 超新星
        ChaoXinXing,
        // 聚变压缩
        JuBianYaSuo,
        // 元素大师
        YuanSuDaShi,
        // 元素协调
        YuanSuXieTiao,
        // 混沌坍缩
        HunDunTanSuo,
        // 纯净爆发
        ChunJingBaoFa,
        // 虚空增幅
        XuKongZengFu,
        // 虚空共鸣
        XuKongGongMing,
        // 雷神之怒
        LeiShenZhiNu,
        // 雷霆领域
        LeiTingLingYu,
        // 永恒冰封
        YongHengBingFeng,
        // 寒冰炼狱
        HanBingLianYu,
        // 地狱熔炉
        DiYuRongLu,
        // 烈焰风暴
        LieYanFengBao,
        // 超级瘟疫
        ChaoJiWenYi,
        // 毒云爆燃
        DuYunBaoRan,
        // 时空回溯（重复）
        ShiKongHuiSu_Repeated,
        // 裂隙增幅
        LieXiZengFu,
        // 镜像大师
        JingXiangDaShi,
        // 完美复制
        WanMeiFuZhi,
        // 流星雨
        LiuXingYu,
        // 星核爆裂
        XingHeBaoLie,
        // 死亡协奏
        SiWangXieZou,
        // 即死韵律
        JiSiYunLu,
        // 天火焚世
        TianHuoFenShi,
        // 烈焰新星
        LieYanXinXing,
        // 极寒领域
        JiHanLingYu,
        // 冰霜死寂
        BingShuangSiJi,
        // 雷神领域
        LeiShenLingYu,
        // 连锁雷暴
        LianSuoLeiBao,
        // 暗影主宰
        AnYingZhuZai,
        // 黑暗统御
        HeiAnTongYu,
        // 剧毒狂潮
        JuDuKuangChao,
        // 毒爆核心
        DuBaoHeXin,
        // 混沌风暴
        HunDunFengBao,
        // 元素聚焦（重复）
        YuanSuJuJiao_Repeated,
        // 时空悖论
        ShiKongBeiLun,
        // 时间锚点
        ShiJianMaoDian,
        // 死亡收割
        SiWangShouGe,
        // 灵魂汲取
        LingHunJiQu,
        // 神圣怒火
        ShenShengNuHuo,
        // 天使之翼
        TianShiZhiYi,
        // 极寒王权
        JiHanWangQuan,
        // 冰霜领域
        BingShuangLingYu,
        // 雷霆之神
        LeiTingZhiShen,
        // 闪电疾行
        ShanDianJiXing,
        // 黑暗大军
        HeiAnDaJun,
        // 暗影契约
        AnYingQiYue,
        // 剧毒炼狱
        JuDuLianYu,
        // 毒爆冲击
        DuBaoChongJi,
        // 混沌融合
        HunDunRongHe,
        // 纯净融合
        ChunJingRongHe,
        // 末日审判
        MoRiShenPan,
        // 死亡领域
        SiWangLingYu
    }
}
