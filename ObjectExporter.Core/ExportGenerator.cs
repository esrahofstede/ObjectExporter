﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using ObjectExporter.Core.Globals;
using ObjectExporter.Core.Models;
using ObjectExporter.Core.Templates;
using ObjectExporter.Core.Templates.Converters;

namespace ObjectExporter.Core
{
    public class ExportGenerator
    {
        private readonly ExportType _type;
        private readonly IEnumerable<ExpressionWithSource> _expressionsWithSources;
        private readonly int _maxDepth;

        private readonly LocalsConverter _localsConverter = new LocalsConverter();
        private readonly CustomExpressionConverter _customExpressionConverter = new CustomExpressionConverter();

        private readonly PropertyAccessibilityChecker _propertyChecker;
        private readonly bool _excludePrivates;

        public ExportGenerator(IEnumerable<ExpressionWithSource> expressionsWithSources,
            AccessibilityRetriever retriever, ExportParamaters exportParamaters)
        {
            _type = exportParamaters.ExportType;
            _expressionsWithSources = expressionsWithSources;
            _maxDepth = exportParamaters.MaxDepth;
            _excludePrivates = exportParamaters.ExludePrivateProperties;
            _propertyChecker = new PropertyAccessibilityChecker(retriever);
        }

        public async Task<Dictionary<string, string>> GenerateTextWithKey(CancellationToken cancellationToken)
        {
            var generatedTaskObjects = new Dictionary<string, Task<string>>();

            foreach (ExpressionWithSource expressionWithSource in _expressionsWithSources)
            {
                Expression expression = expressionWithSource.Expression;
                ExpressionSourceType source = expressionWithSource.Source;

                Task<string> generatedTextTask = GenerateTextAsync(expression, source, cancellationToken);

                string expressionNameResolvedDuplicates;
                if (generatedTaskObjects.Keys.Contains(expression.Name))
                {
                    int count = generatedTaskObjects.Keys.Count(x => x.StartsWith(expression.Name + " (")) + 1;
                    expressionNameResolvedDuplicates = expression.Name + " (" + count + ")";
                }
                else
                {
                    expressionNameResolvedDuplicates = expression.Name;
                }

                generatedTaskObjects.Add(expressionNameResolvedDuplicates, generatedTextTask);
            }

            await Task.WhenAll(generatedTaskObjects.Values);
            Dictionary<string, string> generatedObjects = generatedTaskObjects.ToDictionary(x => x.Key, y => y.Value.Result);

            return generatedObjects;
        }

        public async Task<Dictionary<string, string>> GenerateTextWithKey()
        {
            var generatedTaskObjects = new Dictionary<string, Task<string>>();

            foreach (ExpressionWithSource expressionWithSource in _expressionsWithSources)
            {
                Expression expression = expressionWithSource.Expression;
                ExpressionSourceType source = expressionWithSource.Source;

                Task<string> generatedTextTask = GenerateTextAsync(expression, source);
                generatedTaskObjects.Add(expression.Name, generatedTextTask);
            }

            await Task.WhenAll(generatedTaskObjects.Values);
            Dictionary<string, string> generatedObjects = generatedTaskObjects.ToDictionary(x => x.Key, y => y.Value.Result);

            return generatedObjects;
        }

        public List<string> GenerateText()
        {
            List<string> generatedObjects = new List<string>();

            foreach (ExpressionWithSource expressionWithSource in _expressionsWithSources)
            {
                Expression expression = expressionWithSource.Expression;
                ExpressionSourceType source = expressionWithSource.Source;

                string generatedText = GenerateText(expression, source);
                generatedObjects.Add(generatedText);
            }

            return generatedObjects;
        }

        private string GenerateText(Expression expression, ExpressionSourceType source)
        {
            IGenerator template = GeneratorFactory.CreateGenerator(_type, _propertyChecker);

            //TODO: can most likely remove this templateInitialization and replace with constructor in partial class
            var templateInitialization = new Dictionary<string, object>
            {
                {"objectExpression", expression},
                {"maxDepth", _maxDepth},
                {"excludePrivates", _excludePrivates}
            };

            template.Session = templateInitialization;

            //Set the conversion source
            if (source == ExpressionSourceType.Locals)
            {
                template.Converter = _localsConverter;
            }
            else
            {
                template.Converter = _customExpressionConverter;
            }

            template.Initialize();
            template.Clear();

            string transformedText = template.TransformText();

            return transformedText;
        }

        private Task<string> GenerateTextAsync(Expression expression, ExpressionSourceType source)
        {
            return Task.Run(() => GenerateText(expression, source));
        }

        private Task<string> GenerateTextAsync(Expression expression, ExpressionSourceType source, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (cancellationToken.Register(System.Threading.Thread.CurrentThread.Abort))
                    {
                        return GenerateText(expression, source);
                    }
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
            }, cancellationToken);
        }

        //TODO: convert to extension method
        private Dictionary<string, string> ToDictionaryResolveDuplicates(Dictionary<string, Task<string>> inputDictionary)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            foreach (KeyValuePair<string, Task<string>> kvp in inputDictionary)
            {
                if (dictionary.Keys.Contains(kvp.Key))
                {
                    int count = dictionary.Keys.Count(x => x.StartsWith(kvp.Key + "(")) + 1;
                    dictionary.Add(kvp.Key + " (" + count + ")", kvp.Value.Result);
                }
                else
                {
                    dictionary.Add(kvp.Key, kvp.Value.Result);
                }
            }

            return dictionary;
        }
    }
}
