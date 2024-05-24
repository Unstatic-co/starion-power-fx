using Microsoft.PowerFx;
using Microsoft.PowerFx.Types;
using PowerFxWasm.Model;

namespace PowerFxWasm.Functions
{
	public class UserFunction : ReflectionFunction
        {
					public string jsonUser;
            public UserFunction(string jsonUser)
            : base("User", TypeGenerator.GetTypeFromJson(jsonUser))
            {
							this.jsonUser = jsonUser;
            }

            public FormulaValue Execute()
            {
                return FormulaValueJSON.FromJson(jsonUser);
            }
        }
}