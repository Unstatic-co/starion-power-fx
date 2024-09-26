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

namespace PowerFxWasm
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            Console.SetOut(TextWriter.Null);
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
        [Obsolete]
        public static async Task<string> EvaluateAsync(string context, string expression)
        {
            IReadOnlyList<Token>? tokens = null;
            CheckResult? check = null;
            var cts = new CancellationTokenSource();
            var _timeout = TimeSpan.FromSeconds(2);
            cts.CancelAfter(_timeout);
            try
            {
                var engineContext = new PowerFxEngineContext(context);
                var engine = new PowerFxScopeFactory().GetEngine(engineContext.functions);

                var parameters = (RecordValue)FormulaValueJSON.FromJson(engineContext.jsonContext);

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
                    tokens,
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
                    tokens,
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
        [Obsolete]
        public static async Task<string> BatchEvaluateAsync(string[] contexts, string expression)
        {
            IReadOnlyList<Token>? tokens = null;
            CheckResult? check = null;
            var cts = new CancellationTokenSource();
            var _timeout = TimeSpan.FromSeconds(2);
            cts.CancelAfter(_timeout);
            try
            {
                var engineContext = new PowerFxEngineContext(contexts[0]);
                var firstParameters = (RecordValue)FormulaValueJSON.FromJson(engineContext.jsonContext);
                if (firstParameters == null)
                {
                    firstParameters = RecordValue.Empty();
                }
                
                var engine = new PowerFxScopeFactory().GetEngine(engineContext.functions);
                tokens = engine.Tokenize(expression);
                check = engine.Check(expression, firstParameters.Type, options: new ParserOptions(new CultureInfo("en-US")));
                check.ThrowOnErrors();
                var eval = check.GetEvaluator();
                
                var results = new List<string>();

                foreach (string context in contexts)
                {
                    var currentEngineContext = new PowerFxEngineContext(context);
                    var parameters = (RecordValue)FormulaValueJSON.FromJson(currentEngineContext.jsonContext);
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
                    catch (Exception)
                    {
                        results.Add("");
                    }
                }

                return JsonSerializer.Serialize(new
                {
                    result = results.ToArray(),
                    tokens,
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
                    tokens,
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
        public static string LspAsync(string body)
        {
            var scopeFactory = new PowerFxScopeFactory();

            var sendToClientData = new List<string>();
            var languageServer = new LanguageServer(sendToClientData.Add, scopeFactory);

            try
            {
                languageServer.OnDataReceived(body.ToString());
                return JsonSerializer.Serialize(sendToClientData.ToArray());
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}