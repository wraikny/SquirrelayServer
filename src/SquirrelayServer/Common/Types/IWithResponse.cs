namespace SquirrelayServer.Common
{
    /// <summary>
    /// Response to a send message
    /// </summary>
    public interface IResponse { }

    /// <summary>
    /// T-type response is expected to be returned
    /// </summary>
    public interface IWithResponse<T> where T : IResponse { }
}
