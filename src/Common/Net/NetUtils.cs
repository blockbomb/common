// Copyright Bastian Eicher
// Licensed under the MIT License

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using NanoByte.Common.Native;

namespace NanoByte.Common.Net;

/// <summary>
/// Provides helper methods for the <see cref="System.Net"/> subsystem.
/// </summary>
public static class NetUtils
{
    /// <summary>
    /// Applies environment variable HTTP proxy server configuration if present.
    /// </summary>
    /// <remarks>Uses classic Linux environment variables: http_proxy, http_proxy_user, http_proxy_pass</remarks>
    public static void ApplyProxy()
    {
        string? httpProxy = Environment.GetEnvironmentVariable("http_proxy");
        string? httpProxyUser = Environment.GetEnvironmentVariable("http_proxy_user");
        string? httpProxyPass = Environment.GetEnvironmentVariable("http_proxy_pass");
        if (!string.IsNullOrEmpty(httpProxy))
        {
            WebRequest.DefaultWebProxy = string.IsNullOrEmpty(httpProxyUser)
                ? new WebProxy(httpProxy)
                : new WebProxy(httpProxy) {Credentials = new NetworkCredential(httpProxyUser, httpProxyPass)};
        }
    }

    /// <summary>
    /// Enables TLS 1.2 and TLS 1.3 support if available.
    /// </summary>
    public static void ConfigureTls()
    {
        try
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; // TLS 1.2
        }
        catch (NotSupportedException)
        {
            Log.Warn(Resources.Tls12SupportMissing);
        }

        try
        {
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)12288; // TLS 1.3
        }
        catch (NotSupportedException)
        {}
    }

    /// <summary>
    /// Makes the SSL validation subsystem trust a set of certificates, even if their certificate chain is not trusted.
    /// </summary>
    /// <param name="publicKeys">The public keys of the certificates to trust.</param>
    /// <remarks>This method affects the global state of the <see cref="AppDomain"/>. Calling it more than once is not cumulative and will overwrite previous certificates. You should call this method exactly once near the beginning of your application.</remarks>
    public static void TrustCertificates(params string[] publicKeys)
    {
        #region Sanity checks
        if (publicKeys == null) throw new ArgumentNullException(nameof(publicKeys));
        #endregion

        ServicePointManager.ServerCertificateValidationCallback = (_, certificate, _, sslPolicyErrors)
            => sslPolicyErrors == SslPolicyErrors.None
            || sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && certificate != null && publicKeys.Contains(certificate.GetPublicKeyString());
    }

    /// <summary>
    /// Determines whether an internet connection is currently available. May return false positives.
    /// </summary>
    public static bool IsInternetConnected => WindowsUtils.IsWindowsNT
        ? SafeNativeMethods.InternetGetConnectedState(out _, 0)
        : NetworkInterface.GetIsNetworkAvailable();

    [SuppressUnmanagedCodeSecurity]
    private static class SafeNativeMethods
    {
        [DllImport("wininet", SetLastError = true)]
        public static extern bool InternetGetConnectedState(out int lpdwFlags, int dwReserved);
    }
}