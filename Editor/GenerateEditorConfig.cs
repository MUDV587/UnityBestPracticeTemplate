using UnityEditor;
using UnityEngine;
using System.IO;

namespace Unity.BestPractices.Editor
{
    /// <summary>
    /// Creates an <c>.editorconfig</c> file at the project root that matches the package's C# style guide.
    /// Accessible via the menu: <b>Window → Best Practices → Generate .editorconfig</b>.
    /// </summary>
    /// <remarks>
    /// The generated config enforces naming conventions, formatting rules, and code style
    /// preferences that align with the Best Practices C# coding standard. IDEs such as
    /// Rider, Visual Studio, and VS Code will automatically pick up and enforce these rules.
    /// </remarks>
    public static class GenerateEditorConfig
    {
        /// <summary>
        /// Creates or overwrites the <c>.editorconfig</c> file at the Unity project root.
        /// Prompts the user for confirmation if the file already exists.
        /// </summary>
        [MenuItem("Tools/Unity Project Configurator/Code/Generate .editorconfig", false, 102)]
        public static void Execute()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string editorConfigPath = Path.Combine(projectRoot, ".editorconfig");

            if (File.Exists(editorConfigPath))
            {
                bool overwrite = EditorUtility.DisplayDialog(
                    "Generate .editorconfig",
                    ".editorconfig already exists. Overwrite it?",
                    "Overwrite",
                    "Cancel"
                );
                if (!overwrite) return;
            }

            string content = @"# EditorConfig — Unity Best Practices
# https://editorconfig.org
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true
max_line_length = 120

[*.cs]
# ── Naming conventions ────────────────────────────────────────────────────────
# Shared styles
dotnet_naming_style.pascal_case.capitalization             = pascal_case
dotnet_naming_style.camel_case.capitalization              = camel_case

# Private instance fields: _camelCase  (e.g. _health, _isActive)
dotnet_naming_rule.private_instance_fields.symbols         = private_instance_fields
dotnet_naming_rule.private_instance_fields.style           = underscore_prefix
dotnet_naming_rule.private_instance_fields.severity        = warning
dotnet_naming_symbols.private_instance_fields.applicable_kinds          = field
dotnet_naming_symbols.private_instance_fields.applicable_accessibilities = private, protected
dotnet_naming_style.underscore_prefix.capitalization       = camel_case
dotnet_naming_style.underscore_prefix.required_prefix      = _

# Private static fields: _camelCase  (e.g. _sharedCount, _instance)
dotnet_naming_rule.private_static_fields.symbols           = private_static_fields
dotnet_naming_rule.private_static_fields.style             = underscore_prefix
dotnet_naming_rule.private_static_fields.severity          = warning
dotnet_naming_symbols.private_static_fields.applicable_kinds            = field
dotnet_naming_symbols.private_static_fields.applicable_accessibilities  = private, protected
dotnet_naming_symbols.private_static_fields.required_modifiers          = static

# Constants: PascalCase  (e.g. MaxCount, DefaultSpeed)
# Accessibility list is explicit (all six levels) so this rule is more specific
# than the static-field rule below: const is implicitly static, and Roslyn
# applies the most specific matching rule (see learn.microsoft.com naming-rules).
dotnet_naming_rule.constants.symbols                       = constants
dotnet_naming_rule.constants.style                         = pascal_case
dotnet_naming_rule.constants.severity                      = warning
dotnet_naming_symbols.constants.applicable_kinds           = field
dotnet_naming_symbols.constants.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.constants.required_modifiers         = const

# Static readonly fields: PascalCase
dotnet_naming_rule.static_readonly_fields.symbols          = static_readonly_fields
dotnet_naming_rule.static_readonly_fields.style            = pascal_case
dotnet_naming_rule.static_readonly_fields.severity         = warning
dotnet_naming_symbols.static_readonly_fields.applicable_kinds          = field
dotnet_naming_symbols.static_readonly_fields.applicable_accessibilities = private, protected
dotnet_naming_symbols.static_readonly_fields.required_modifiers        = static, readonly

# Public fields: PascalCase
dotnet_naming_rule.public_fields.symbols                   = public_fields
dotnet_naming_rule.public_fields.style                     = pascal_case
dotnet_naming_rule.public_fields.severity                  = warning
dotnet_naming_symbols.public_fields.applicable_kinds       = field
dotnet_naming_symbols.public_fields.applicable_accessibilities = public

# Properties and methods: PascalCase
dotnet_naming_rule.properties.symbols                      = properties
dotnet_naming_rule.properties.style                        = pascal_case
dotnet_naming_rule.properties.severity                     = warning
dotnet_naming_symbols.properties.applicable_kinds          = property

dotnet_naming_rule.methods.symbols                         = methods
dotnet_naming_rule.methods.style                           = pascal_case
dotnet_naming_rule.methods.severity                        = warning
dotnet_naming_symbols.methods.applicable_kinds             = method

# ── Formatting (Allman style) ─────────────────────────────────────────────────
csharp_new_line_before_open_brace                          = all
csharp_new_line_before_else                                = true
csharp_new_line_before_catch                               = true
csharp_new_line_before_finally                             = true
csharp_indent_case_contents                                = true
csharp_indent_switch_labels                                = true

# Spaces
csharp_space_after_cast                                    = false
csharp_space_after_keywords_in_control_flow_statements     = true
csharp_space_between_parentheses                           = false
csharp_space_before_colon_in_inheritance_clause            = true
csharp_space_after_colon_in_inheritance_clause             = true
csharp_space_around_binary_operators                       = before_and_after

# ── Using directives ──────────────────────────────────────────────────────────
csharp_using_directive_placement                           = outside_namespace
dotnet_sort_system_directives_first                        = true
dotnet_separate_import_directive_groups                    = true

# ── Expression preferences ────────────────────────────────────────────────────
csharp_prefer_braces                                       = true:suggestion
csharp_style_var_for_built_in_types                        = true:suggestion
csharp_style_var_when_type_is_apparent                     = true:suggestion
csharp_style_var_elsewhere                                 = true:suggestion
csharp_style_expression_bodied_methods                     = false:suggestion
csharp_style_expression_bodied_constructors                = false:suggestion

[*.{xml,json,yaml,yml,asmdef,asmref}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
";

            File.WriteAllText(editorConfigPath, content);
            Debug.Log($"[BestPractice] .editorconfig created at: {editorConfigPath}");
        }
    }
}

