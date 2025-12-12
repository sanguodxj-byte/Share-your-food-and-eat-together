using RimWorld;
using Verse;

namespace RimTalkSocialDining
{
    /// <summary>
    /// 静态 Def 引用类，用于代码中访问自定义的 JobDef
    /// </summary>
    [DefOf]
    public static class SocialDiningDefOf
    {
        public static JobDef SocialDine;

        static SocialDiningDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SocialDiningDefOf));
        }
    }
}
