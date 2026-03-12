namespace ACS.Communication.Http.uHttpSharp
{
    public interface IHttpMethodProvider
    {
        HttpMethods Provide(string name);
    }
}