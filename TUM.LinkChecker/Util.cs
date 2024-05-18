namespace TUM.LinkChecker;

internal static class Util
{
    internal static CancellationToken CreateCancellationTokenFromSeconds(double timeOut)
    {
        return new CancellationTokenSource(TimeSpan.FromSeconds(timeOut)).Token;
    }
    internal static CancellationToken CreateCancellationTokenFromSeconds(double timeOut, CancellationToken other)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(CreateCancellationTokenFromSeconds(timeOut), other).Token;
    }
}