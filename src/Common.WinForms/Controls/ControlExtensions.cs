// Copyright Bastian Eicher
// Licensed under the MIT License

namespace NanoByte.Common.Controls;

/// <summary>
/// Provides extension methods for <see cref="Control"/>s.
/// </summary>
public static class ControlExtensions
{
#if !NET6_0_OR_GREATER
    /// <summary>
    /// Executes the given <paramref name="action"/> on the thread that owns this control and returns immediately.
    /// </summary>
    public static void BeginInvoke(this Control control, Action action)
    {
        #region Sanity checks
        if (control == null) throw new ArgumentNullException(nameof(control));
        if (action == null) throw new ArgumentNullException(nameof(action));
        #endregion

        control.BeginInvoke(action);
    }

    /// <summary>
    /// Executes the given <paramref name="action"/> on the thread that owns this control and blocks until it is complete.
    /// </summary>
    public static void Invoke(this Control control, Action action)
    {
        #region Sanity checks
        if (control == null) throw new ArgumentNullException(nameof(control));
        if (action == null) throw new ArgumentNullException(nameof(action));
        #endregion

        control.Invoke(action);
    }

    /// <summary>
    /// Executes the given <paramref name="action"/> on the thread that owns this control and blocks until it is complete.
    /// </summary>
    /// <returns>The return value of the <paramref name="action"/>.</returns>
    public static T Invoke<T>(this Control control, Func<T> action)
    {
        #region Sanity checks
        if (control == null) throw new ArgumentNullException(nameof(control));
        if (action == null) throw new ArgumentNullException(nameof(action));
        #endregion

        return (T)control.Invoke(action);
    }
#endif

    /// <summary>
    /// Returns the current auto-scaling factor.
    /// </summary>
    /// <remarks>
    /// Assumes the default <see cref="ContainerControl.AutoScaleDimensions"/> of 6, 13.
    /// Unlike <see cref="ContainerControl.AutoScaleFactor"/> this will retain the correct factor even after <see cref="ContainerControl.PerformAutoScale"/> has run.
    /// </remarks>
    public static SizeF GetScaleFactor(this ContainerControl control)
    {
        #region Sanity checks
        if (control == null) throw new ArgumentNullException(nameof(control));
        #endregion

        return new(control.AutoScaleDimensions.Width / 6F, control.AutoScaleDimensions.Height / 13F);
    }

    /// <summary>
    /// Scales a <see cref="Size"/> according to the current auto-scaling factor.
    /// </summary>
    /// <param name="size">The size to scale.</param>
    /// <param name="control">The control to get the scaling factor from.</param>
    public static Size ApplyScale(this Size size, ContainerControl control)
        => size.MultiplyAndRound(control.GetScaleFactor());
}
