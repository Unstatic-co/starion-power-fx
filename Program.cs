using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Core.Utils;
using Microsoft.PowerFx.LanguageServerProtocol;
using Microsoft.PowerFx.Syntax;
using Microsoft.PowerFx.Types;
using PowerFxWasm.Commons;
using PowerFxWasm.Model;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using  System.Threading;

namespace PowerFxWasm
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            ////builder.RootComponents.Add<App>("#app");
            ////builder.RootComponents.Add<HeadOutlet>("head::after");

            //builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new NodeConverter<TexlNode>(),
                new NodeConverter<VariadicOpNode>(),
                new NodeConverter<ListNode>(),
                new NodeConverter<CallNode>(),
                new NodeConverter<Identifier>(),
                new NodeConverter<DName>()
            }
        };
        [JSInvokable]
        public static async Task<string> EvaluateAsync(string context, string expression)
        {
            IReadOnlyList<Token> tokens = null;
            CheckResult check = null;
            var cts = new CancellationTokenSource();
            var _timeout = TimeSpan.FromSeconds(2);
            cts.CancelAfter(_timeout);
            try
            {
                var engine = new PowerFxScopeFactory().GetEngine();

                var parameters = (RecordValue)FormulaValueJSON.FromJson(context);

                if (parameters == null)
                {
                    parameters = RecordValue.Empty();
                }

                tokens = engine.Tokenize(expression);
                check = engine.Check(expression, parameters.Type, options: new ParserOptions(new CultureInfo("en-US")));
                check.ThrowOnErrors();
                var eval = check.GetEvaluator();
                var result = await eval.EvalAsync(cts.Token, parameters);
                var resultString = PowerFxHelper.TestToString(result);

                return JsonSerializer.Serialize(new
                {
                    result = resultString,
                    tokens = tokens,
                    parse = JsonSerializer.Serialize(check.Parse.Root, _jsonSerializerOptions)
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    tokens = tokens,
                    parse = check != null ? JsonSerializer.Serialize(check.Parse.Root, _jsonSerializerOptions) : null
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            finally
            {
                cts.Dispose();
            }
        }

        [JSInvokable]
        public static async Task<string> BatchEvaluateAsync(string[] contexts, string expression)
        {
            IReadOnlyList<Token> tokens = null;
            CheckResult check = null;
            var cts = new CancellationTokenSource();
            var _timeout = TimeSpan.FromSeconds(2);
            cts.CancelAfter(_timeout);
            try
            {
                var firstParameters = (RecordValue)FormulaValueJSON.FromJson(contexts[0]);
                if (firstParameters == null)
                {
                    firstParameters = RecordValue.Empty();
                }

                var engine = new PowerFxScopeFactory().GetEngine();
                tokens = engine.Tokenize(expression);
                check = engine.Check(expression, firstParameters.Type, options: new ParserOptions(new CultureInfo("en-US")));
                check.ThrowOnErrors();
                var eval = check.GetEvaluator();
                
                var results = new List<string>();

                foreach (string context in contexts)
                {
                    var parameters = (RecordValue)FormulaValueJSON.FromJson(context);
                    if (parameters == null)
                    {
                        parameters = RecordValue.Empty();
                    }

                    try
                    {
                        var result = await eval.EvalAsync(cts.Token, parameters);
                        var resultString = PowerFxHelper.TestToString(result);
                        results.Add(resultString);
                    }
                    catch (Exception e)
                    {
                        results.Add(null);
                    }
                }

                return JsonSerializer.Serialize(new
                {
                    result = results.ToArray(),
                    tokens = tokens,
                    parse = JsonSerializer.Serialize(check.Parse.Root, _jsonSerializerOptions)
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    tokens = tokens,
                    parse = check != null ? JsonSerializer.Serialize(check.Parse.Root, _jsonSerializerOptions) : null
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            finally
            {
                cts.Dispose();
            }
        }

        [JSInvokable]
        public static async Task<string> LspAsync(string body)
        {
            var scopeFactory = new PowerFxScopeFactory();

            var sendToClientData = new List<string>();
            var languageServer = new LanguageServer((string data) => sendToClientData.Add(data), scopeFactory);

            try
            {
                languageServer.OnDataReceived(body.ToString());
                return JsonSerializer.Serialize(sendToClientData.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}