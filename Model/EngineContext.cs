using System.Text.Json;
using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using PowerFxWasm.Functions;

namespace PowerFxWasm.Model
{
	public class PowerFxEngineContext {
		public ReflectionFunction[] functions;
		public string jsonContext;
		public PowerFxEngineContext(string jsonContext) {
			functions = Array.Empty<ReflectionFunction>();
			var context = new Context(jsonContext);
			
			this.jsonContext = context.jsonContext;
			
			if(context.jsonCurrentUser != null) {
				functions = new ReflectionFunction[] { new UserFunction(context.jsonCurrentUser) };
			}
		}
	}

	public class Context {
		public string jsonContext;
		public string? jsonCurrentUser;
		
		public Context(string json) {
			var rootContext = (RecordValue)FormulaValueJSON.FromJson(json);
			var context = rootContext.GetField("Context");
			var currentUser = rootContext.GetField("CurrentUser");

			if(context != null && context.Type != FormulaType.Blank) {
				jsonContext = JsonSerializer.Serialize(context.ToObject());
			} else {
				jsonContext = "{}";
			}

			if(currentUser != null && currentUser.Type != FormulaType.Blank) {
				jsonCurrentUser = JsonSerializer.Serialize(currentUser.ToObject());
			}
		}
	}
}