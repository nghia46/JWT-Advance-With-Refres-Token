namespace JWT_Advance
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AllowAnonymousRefreshTokenAttribute : Attribute { }
}
