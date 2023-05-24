using spaghetto;

namespace spaghettoWeb.spaglib
{
    public class Request
    {
        public static SClass CreateClass()
        {
            var @class = new SClass("Request");
            
            @class.InstanceBaseTable.Add(("$$ctor", new SNativeFunction(
                impl: (Scope scope, List<SValue> args) =>
                {
                    if (args[0] is not SClassInstance self) throw new Exception("unexpected error!");
                    return self;
                },
                expectedArgs: new() { "self" }
            )));

            
        }
    }
}
