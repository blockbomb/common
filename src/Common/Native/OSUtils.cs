// Copyright Bastian Eicher
// Licensed under the MIT License

using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace NanoByte.Common.Native;

/// <summary>
/// Provides helper methods related to operating system functionality across multiple platforms.
/// </summary>
public static class OSUtils
{
    private static readonly Regex
        _envVariableLongStyle = new(@"\${([^{}]+)}"),
        _envVariableShortStyle = new(@"\$([^{}\$\s\\/-]+)");

    /// <summary>
    /// Expands/substitutes any Unix-style environment variables in the string.
    /// </summary>
    /// <param name="value">The string containing variables to be expanded.</param>
    /// <param name="variables">The list of variables available for expansion.</param>
    /// <remarks>Supports default values for unset variables (<c>${VAR-default}</c>) and for unset or empty variables (<c>${VAR:-default}</c>).</remarks>
    public static string ExpandVariables(string value, IDictionary<string, string?> variables)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (variables == null) throw new ArgumentNullException(nameof(variables));

        string intermediate = _envVariableLongStyle.Replace(value, x =>
        {
            if (x.Index >= 1 && value[x.Index - 1] == '$') // Treat $$ as escaping
                return "{" + x.Groups[1].Value + "}";

            var parts = x.Groups[1].Value.Split(new[] {":-"}, StringSplitOptions.None);
            if (variables.TryGetValue(parts[0], out string? ret) && !string.IsNullOrEmpty(ret))
                return ret;
            else if (parts.Length > 1)
                return StringUtils.Join(":-", parts.Skip(1));
            else
            {
                parts = x.Groups[1].Value.Split('-');
                if (variables.TryGetValue(parts[0], out ret) && ret != null)
                    return ret;
                else if (parts.Length > 1)
                    return StringUtils.Join("-", parts.Skip(1));
                else
                {
                    Log.Warn($"Variable '{parts[0]}' not set. Defaulting to empty string.");
                    return "";
                }
            }
        });

        return _envVariableShortStyle.Replace(intermediate, x =>
        {
            if (x.Index >= 1 && value[x.Index - 1] == '$') // Treat $$ as escaping
                return x.Groups[1].Value;

            string key = x.Groups[1].Value;
            if (variables.TryGetValue(key, out string? ret) && ret != null)
                return ret;
            else
            {
                Log.Warn($"Variable '{key}' not set. Defaulting to empty string.");
                return "";
            }
        });
    }

    /// <summary>
    /// Expands/substitutes any Unix-style environment variables in the string.
    /// </summary>
    /// <param name="value">The string containing variables to be expanded.</param>
    /// <param name="variables">The list of variables available for expansion.</param>
    /// <remarks>Supports default values for unset variables (<c>${VAR-default}</c>) and for unset or empty variables (<c>${VAR:-default}</c>).</remarks>
    public static string ExpandVariables(string value, StringDictionary variables)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        if (variables == null) throw new ArgumentNullException(nameof(variables));

        var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (string key in variables.Keys)
            dictionary[key] = variables[key]!;

        return ExpandVariables(value, dictionary);
    }
}
