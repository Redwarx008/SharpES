﻿namespace SharpES.Core
{
    public delegate Value ForeignFunctionDelegate(IList<Value> args);

    /// <summary>
    ///  Used as foreign module function and foreign class static methods.
    /// </summary>
    public class ForeignFunction
    {
        public string Name { get; private set; }
        public ForeignFunctionDelegate Function { get; private set; }
        public ForeignFunction(string name, ForeignFunctionDelegate functionDelegate)
        {
            Name = name;
            Function = functionDelegate;
        }

        public override string ToString()
        {
            return $"<Fn {Name}>";
        }
    }
}
