﻿using Esprima;
using Jint;
using Jint.Native;
using Json;
using System.Collections.Generic;
using Esprima.Ast;
using Jint.Runtime.Modules;

namespace Hercules.Scripting.JavaScript
{
    public interface IJsModuleProvider
    {
        string GetModuleCode(string name);
    }

    internal class JsModuleLoader : IModuleLoader
    {
        private readonly IJsModuleProvider provider;

        public JsModuleLoader(IJsModuleProvider moduleProvider)
        {
            this.provider = moduleProvider;
        }

        public ResolvedSpecifier Resolve(string? referencingModuleLocation, string specifier)
        {
            return new ResolvedSpecifier(specifier, specifier, null, SpecifierType.Bare);
        }

        public Module LoadModule(Engine engine, ResolvedSpecifier resolved)
        {
            return new JavaScriptParser().ParseModule(provider.GetModuleCode(resolved.Specifier), resolved.Specifier);
        }
    }

    public class JsHost
    {
        readonly Engine engine;

        public JsHost(IJsModuleProvider? moduleProvider = null)
        {
            Options GetOptions(Options options)
            {
                if (moduleProvider != null)
                    options = options.EnableModules(new JsModuleLoader(moduleProvider));
                return options;
            }
            engine = new Engine(options => GetOptions(options));
        }

        public ImmutableJson GetJsonValue(string name)
        {
            return JsValueConverter.ToJsonValue(engine.GetValue(name));
        }

        public JsHost SetJsonValue(string name, ImmutableJson json)
        {
            engine.SetValue(name, JsValueConverter.FromJsonValue(json, engine));
            return this;
        }

        public JsHost SetValue(string name, object value)
        {
            engine.SetValue(name, value);
            return this;
        }

        public JsValue JsonToJsValue(ImmutableJson? json)
        {
            return JsValueConverter.FromJsonValue(json, engine);
        }

        public ImmutableJson JsValueToJson(JsValue value, bool supportDateTime = false)
        {
            return JsValueConverter.ToJsonValue(value, supportDateTime);
        }

        public JsHost Execute(string script)
        {
            engine.Execute(script);
            return this;
        }

        public JsHost Include(string filename)
        {
            var script = System.IO.File.ReadAllText(filename);
            return Execute(script);
        }

        public static IReadOnlyList<ParserException>? SyntaxCheck(string script)
        {
            try
            {
                var parser = new JavaScriptParser(new ParserOptions { Tolerant = true });
                parser.ParseScript(script, strict: true);
                return null;
            }
            catch (ParserException ex)
            {
                return new[] { ex };
            }
        }
    }
}