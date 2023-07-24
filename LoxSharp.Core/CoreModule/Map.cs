﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    public class Map : Class
    {
        public Map()
            : base("Map")
        {
            Methods[nameof(Init)] = new Value(new ForeignMethod(nameof(Init), Init));
            Methods[nameof(Get)] = new Value(new ForeignMethod(nameof(Get), Get));
            Methods[nameof(Add)] = new Value(new ForeignMethod(nameof(Add), Add));
            Methods[nameof(Remove)] = new Value(new ForeignMethod(nameof(Remove), Remove)); 
            Methods[nameof(ContainsKey)] = new Value(new ForeignMethod(nameof(ContainsKey), ContainsKey));
            Methods[nameof(ContainsValue)] = new Value(new ForeignMethod(nameof(ContainsValue), ContainsValue));
            Methods[nameof(Clear)] = new Value(new ForeignMethod(nameof(Clear), Clear));
            Methods[nameof(Count)] = new Value(new ForeignMethod(nameof(Count), Count));    
        }

        public override ClassInstance CreateInstance() => new MapInstance(this);

        private Value Init(ClassInstance instance, Value[] args)
        {
            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            for(int i = 0; i < args.Length; i += 2)
            {
                entries[args[i]] = args[i + 1];
            }
            return new Value(instance);
        }

        private Value Get(ClassInstance instance, Value[] args) 
        {
            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            if (!entries.TryGetValue(args[0], out var value)) 
            {
                throw new RuntimeException("Could not find element with specified key.");
            }
            return value;
        }

        private Value Add(ClassInstance instance, Value[] args) 
        {
            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            entries[args[0]] = args[1];
            return Value.NUll;
        }

        private Value ContainsKey(ClassInstance instance, Value[] args)
        {
            if (args.Length > 1)
            {
                throw new RuntimeException("too many parameters.");
            }
            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            return new Value(entries.ContainsKey(args[0]));
        }

        private Value ContainsValue(ClassInstance instance, Value[] args) 
        {
            if (args.Length > 1)
            {
                throw new RuntimeException("too many parameters.");
            }
            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            return new Value((entries.ContainsValue(args[0]))); 
        }

        private Value Remove(ClassInstance instance, Value[] args)
        {
            if (args.Length > 1)
            {
                throw new RuntimeException("too many parameters.");
            }

            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            return new Value(entries.Remove(args[0]));
        }

        private Value Clear(ClassInstance instance, Value[] args)
        {
            if (args.Length > 0)
            {
                throw new RuntimeException("too many parameters.");
            }

            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            entries.Clear();    
            return Value.NUll;
        }

        private Value Count(ClassInstance instance, Value[] args)
        {
            if (args.Length > 0)
            {
                throw new RuntimeException("too many parameters.");
            }

            Dictionary<Value, Value> entries = ((MapInstance)instance).Entries;
            return new Value(entries.Count());
        }
    }
}