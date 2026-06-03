using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace endfield_player_position_display
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
            TryUseUtf8ConsoleEncoding();
            base.OnStartup(e);
        }

        private static Assembly ResolveEmbeddedAssembly(object sender, ResolveEventArgs args)
        {
            string assemblyName = new AssemblyName(args.Name).Name;
            if (!string.Equals(assemblyName, "QRCoder", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Assembly currentAssembly = typeof(App).Assembly;
            using (Stream stream = currentAssembly.GetManifestResourceStream("EmbeddedAssemblies.QRCoder.dll"))
            {
                if (stream == null)
                {
                    return null;
                }

                byte[] assemblyBytes = new byte[stream.Length];
                int offset = 0;
                while (offset < assemblyBytes.Length)
                {
                    int read = stream.Read(assemblyBytes, offset, assemblyBytes.Length - offset);
                    if (read == 0)
                    {
                        break;
                    }

                    offset += read;
                }

                return Assembly.Load(assemblyBytes);
            }
        }

        private static void TryUseUtf8ConsoleEncoding()
        {
            try
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }
        }
    }
}
