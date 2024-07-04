namespace Shop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureKestrel(serverOptions =>
                    {
                        serverOptions.ConfigureHttpsDefaults(options =>
                        {
                            var sslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                            var sslProtocolsEnv = Environment.GetEnvironmentVariable("SSL_PROTOCOLS");
                            if (!string.IsNullOrEmpty(sslProtocolsEnv) && Enum.TryParse(sslProtocolsEnv, out System.Security.Authentication.SslProtocols parsedSslProtocols))
                            {
                                sslProtocols = parsedSslProtocols;
                            }

                            options.SslProtocols = sslProtocols;
                        });
                    });
                });
    }
}
