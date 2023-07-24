﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoxSharp.Core
{
    internal class Array : Class
    {
        public Array()
            :base("Array")
        {
            Methods[nameof(Init)] = new Value(new ForeignMethod(nameof(Init), Init));
            Methods[nameof(Count)] = new Value(new ForeignMethod(nameof(Count), Count));
            Methods[nameof(Add)] = new Value(new ForeignMethod(nameof(Add), Add));
            Methods[nameof(Clear)] = new Value(new ForeignMethod(nameof(Clear), Clear));
            Methods[nameof(RemoveAt)] = new Value(new ForeignMethod(nameof(RemoveAt), RemoveAt));
        }

        public override ClassInstance CreateInstance() => new ArrayInstance(this);

        private Value Init(ClassInstance instance, Value[] args)
        {
            List<Value> list = ((ArrayInstance)instance).Values;
            list.AddRange(args);
            // Constructor should return an instance.
            return new Value(instance);
        }

        private Value Count(ClassInstance instance, Value[] args)
        {
            return new Value(((ArrayInstance)instance).Values.Count);
        }

        private Value Add(ClassInstance instance, Value[] args)
        {
            ((ArrayInstance)instance).Values.Add(args[0]);
            return Value.NUll;
        }

        private Value Get(ClassInstance instance, Value[] args) 
        {
            List<Value> array = ((ArrayInstance)instance).Values;
            if (!args[0].IsNumber)
            {
                throw new ForeignRuntimeException("Index must be a number.");  
            }

            int index = (int)args[0].AsDouble;
            if (index < 0 || index >= array.Count)
            {
                throw new ForeignRuntimeException("index out of bounds.");
            }

            return array[index];    
        }

        private Value Clear(ClassInstance instance, Value[] args)
        {
            List<Value> array = ((ArrayInstance)instance).Values;
            array.Clear();
            return Value.NUll;
        }

        private Value RemoveAt(ClassInstance instance, Value[] args)
        {
            List<Value> array = ((ArrayInstance)instance).Values;
            if (!args[0].IsNumber)
            {
                throw new ForeignRuntimeException("Index must be a number.");
            }

            int index = (int)args[0].AsDouble;
            if (index < 0 || index >= array.Count)
            {
                throw new ForeignRuntimeException("index out of bounds.");
            }

            array.RemoveAt(index);
            return Value.NUll;
        }
    }
}
