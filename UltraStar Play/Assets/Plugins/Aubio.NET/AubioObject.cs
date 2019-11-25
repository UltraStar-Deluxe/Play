using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Aubio.NET
{
    public abstract class AubioObject : IDisposable
    {
        internal AubioObject(bool isDisposable = true)
        {
            // make this public object not inheritable

            // handle some special cases
            IsDisposable = isDisposable;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool IsDisposable { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!IsDisposable)
                return; // do not flag as disposed

            if (IsDisposed)
                return;

            DisposeNative();

            if (disposing)
            {
                // nothing
            }

            IsDisposed = true;
        }

        protected abstract void DisposeNative();

        ~AubioObject()
        {
            Dispose(false);
        }

        #region Native side

#if ANYCPU_LOADING_STRATEGY

        static AubioObject()
        {
            // add DLLs directory according current configuration

            var directory = GetDependenciesDirectory();

            if (string.IsNullOrEmpty(directory))
                return; // dependencies are in application directory

            var path = Path.Combine(Environment.CurrentDirectory, directory);

            var cookie = NativeMethods.AddDllDirectory(path);
            if (cookie == IntPtr.Zero)
                throw new Win32Exception();

            Cookie = cookie;
        }

        private static readonly IntPtr Cookie;

        [UsedImplicitly]
        private static readonly Destructor__ Destructor = new Destructor__();

        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
        private static string GetDependenciesDirectory([NotNull] string x86 = "x86", [NotNull] string x64 = "x64")
        {
            if (string.IsNullOrWhiteSpace(x86))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(x86));

            if (string.IsNullOrWhiteSpace(x64))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(x64));

            var assembly = Assembly.GetEntryAssembly();

            var module = assembly.Modules.Single();
            module.GetPEKind(out var kind, out var _);

            if (!kind.HasFlag(PortableExecutableKinds.ILOnly))
                throw new PlatformNotSupportedException(); // guard

            if (kind.HasFlag(PortableExecutableKinds.Required32Bit) || kind.HasFlag(PortableExecutableKinds.PE32Plus))
                return string.Empty; // in application directory

            return kind.HasFlag(PortableExecutableKinds.Preferred32Bit) ? x86 : x64;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private sealed class Destructor__
        {
            ~Destructor__()
            {
                // TODO this is not triggerred, at least on UWP

                if (!NativeMethods.RemoveDllDirectory(Cookie))
                    throw new Win32Exception();
            }
        }
#endif

        #endregion
    }
}