using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitBlurredBackground
{
    public static class UIDocumentHelper
    {
        private static Type _rootElementType;
        private static FieldInfo _documentField;

        static UIDocumentHelper()
        {
            _rootElementType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.UIDocumentRootElement");
            if (_rootElementType != null)
            {
                _documentField = _rootElementType.GetField("document", BindingFlags.Public |BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }

        public static UIDocument GetFirstDocument(VisualElement element)
        {
            if (element == null)
                return null;
            
            if (_documentField == null)
                return null;
            
            if (element.panel == null || element.panel.visualTree.childCount == 0)
                return null;

            var firstDocument = element.panel.visualTree.ElementAt(0);
            if (firstDocument.GetType() == _rootElementType)
                return _documentField.GetValue(firstDocument) as UIDocument;
            return null;
        }
    }
 }