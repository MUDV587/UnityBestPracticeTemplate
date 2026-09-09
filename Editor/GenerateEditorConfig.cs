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
# Field rules follow the .NET Runtime team's coding style (the 'Identifier names'
# conventions on learn.microsoft.com, dotnet/docs house style):
#   • public/protected/protected_internal fields           → PascalCase (any modifier)
#   • private/internal constants (const is static+const)   → PascalCase
#   • private/internal static fields                       → s_camelCase
#   • private/internal instance fields                     → _camelCase
# The four non-public rules overlap on purpose; specificity orders them:
# const,static (2 modifiers) > static (1) > none (0), so each field matches the
# most specific rule (see 'Rule order' in learn.microsoft.com naming-rules).
# [ThreadStatic] fields use t_ by the same guide, but naming rules cannot match
# attributes, so that case is by-convention only.
# The severity on each naming rule is only honoured inside IDEs; this line makes
# naming violations also surface in `dotnet build` (CI, compile-time).
dotnet_diagnostic.IDE1006.severity                         = warning

# Shared styles
dotnet_naming_style.pascal_case.capitalization             = pascal_case
dotnet_naming_style.s_camel_case.capitalization            = camel_case
dotnet_naming_style.s_camel_case.required_prefix           = s_
dotnet_naming_style.underscore_camel_case.capitalization   = camel_case
dotnet_naming_style.underscore_camel_case.required_prefix  = _

# Constants (any accessibility): PascalCase  (e.g. MaxCount, DefaultSpeed)
# const is implicitly static, so required_modifiers lists both to make this rule
# strictly more specific than the static-field rules below.
dotnet_naming_rule.constants.symbols                       = constants
dotnet_naming_rule.constants.style                         = pascal_case
dotnet_naming_rule.constants.severity                      = warning
dotnet_naming_symbols.constants.applicable_kinds           = field
dotnet_naming_symbols.constants.applicable_accessibilities = public, internal, private, protected, protected_internal, private_protected
dotnet_naming_symbols.constants.required_modifiers         = const, static

# Non-public static fields: s_camelCase  (e.g. s_instance, s_sharedCount)
dotnet_naming_rule.non_public_static_fields.symbols        = non_public_static_fields
dotnet_naming_rule.non_public_static_fields.style          = s_camel_case
dotnet_naming_rule.non_public_static_fields.severity       = warning
dotnet_naming_symbols.non_public_static_fields.applicable_kinds          = field
dotnet_naming_symbols.non_public_static_fields.applicable_accessibilities = private, internal, private_protected
dotnet_naming_symbols.non_public_static_fields.required_modifiers        = static

# Non-public instance fields: _camelCase  (e.g. _health, _isActive)
dotnet_naming_rule.non_public_instance_fields.symbols      = non_public_instance_fields
dotnet_naming_rule.non_public_instance_fields.style        = underscore_camel_case
dotnet_naming_rule.non_public_instance_fields.severity     = warning
dotnet_naming_symbols.non_public_instance_fields.applicable_kinds          = field
dotnet_naming_symbols.non_public_instance_fields.applicable_accessibilities = private, internal, private_protected

# Public and protected fields: PascalCase (any modifier)  (e.g. MaxCount, DefaultSpeed)
# The runtime guide treats public, protected and protected internal alike
# (all are visible to external callers); private protected groups with private.
dotnet_naming_rule.public_protected_fields.symbols         = public_protected_fields
dotnet_naming_rule.public_protected_fields.style           = pascal_case
dotnet_naming_rule.public_protected_fields.severity        = warning
dotnet_naming_symbols.public_protected_fields.applicable_kinds           = field
dotnet_naming_symbols.public_protected_fields.applicable_accessibilities = public, protected, protected_internal

# Local constants: PascalCase  (e.g. const int MaxRetries = 3)
dotnet_naming_rule.local_constants.symbols                 = local_constants
dotnet_naming_rule.local_constants.style                   = pascal_case
dotnet_naming_rule.local_constants.severity                = warning
dotnet_naming_symbols.local_constants.applicable_kinds     = local
dotnet_naming_symbols.local_constants.required_modifiers   = const

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

