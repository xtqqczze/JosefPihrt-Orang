﻿# `orang sync`

Synchronizes content of two directories\.

[Home](README.md#readme) &#x2022; [Synopsis](#Synopsis) &#x2022; [Arguments](#Arguments) &#x2022; [Options](#Options) &#x2022; [Samples](#Samples)

## Synopsis

```
orang sync [<PATH>]
[   --ask]
[-a|--attributes]         <ATTRIBUTES>
[-b|--attributes-to-skip] <ATTRIBUTES>
[   --compare]            <COMPARE_OPTIONS>
[   --conflict]           <SYNC_CONFLICT_RESOLUTION>
[-c|--content]            <PATTERN> [<PATTERN_OPTIONS>]
[-y|--display]            <DISPLAY_OPTIONS>
[-d|--dry-run]
[   --encoding]           <ENCODING>
[-e|--extension]          EXT1[,EXT2] [<EXTENSION_OPTIONS>]
[-h|--help]
[-t|--highlight]          <HIGHLIGHT>
[-i|--include-directory]  <PATTERN> [<PATTERN_OPTIONS>]
[-m|--max-count]          <MAX_OPTIONS>
[-n|--name]               <PATTERN> [<PATTERN_OPTIONS>]
[   --no-recurse]
[-o|--output]             <PATH> [<OUTPUT_OPTIONS>]
[   --paths]              <PATH>
[   --paths-from]         <FILE_PATH>
[   --progress]
[-p|--properties]         <FILE_PROPERTIES>
[   --second]             <DIRECTORY_PATH>
[-s|--sort]               <SORT_OPTIONS>
[-v|--verbosity]          <VERBOSITY>
```

## Arguments

**`<PATH>`**

Path to a first directory to be synchronized and optionally a second directory\.

## Options

##### `[--ask]`

Ask for a permission to synchronize file or directory\.

##### `[-a|--attributes] <ATTRIBUTES>`

File attributes that are required\.

[\<ATTRIBUTES>](OptionValues.md#attributes): `archive`, `compressed`, `d[irectory]`, `e[mpty]`, `encrypted`, `f[ile]`, `h[idden]`, `normal`, `offline`, `r[ead-only]`, `r[eparse-]p[oint]`, `s[ystem]`, `temporary`\.

##### `[-b|--attributes-to-skip] <ATTRIBUTES>`

File attributes that should be skipped\.

[\<ATTRIBUTES>](OptionValues.md#attributes): `archive`, `compressed`, `e[mpty]`, `encrypted`, `h[idden]`, `normal`, `offline`, `r[ead-only]`, `r[eparse-]p[oint]`, `s[ystem]`, `temporary`\.

##### `[--compare] <COMPARE_OPTIONS>`

File properties to be compared\.

[\<COMPARE_OPTIONS>](OptionValues.md#compare_options): `n[one]`, `a[ttributes]`, `c[ontent]`, `m[odified-]t[ime]`, `s[ize]`\.

##### `[--conflict] <SYNC_CONFLICT_RESOLUTION>`

Action to choose if a file or directory exists in one directory and it is missing in the second directory\.

[\<SYNC_CONFLICT_RESOLUTION>](OptionValues.md#sync_conflict_resolution): `a[sk]`, `f[irst-wins]`, `s[econd-wins]`\.

##### `[-c|--content] <PATTERN> [<PATTERN_OPTIONS>]`

Regular expression for files' content\.

[\<PATTERN_OPTIONS>](OptionValues.md#pattern_options): `compiled`, `c[ulture-]i[nvariant]`, `e[cma-]s[cript]`, `e[nds-]w[ith]`, `e[quals]`, `n [explicit-capture]`, `f[rom-file]`, `g[roup]=<GROUP_NAME>`, `i[gnore-case]`, `x [ignore-pattern-whitespace]`, `li[st]`, `length=<NUM>`, `l[ist-]s[eparator]`, `l[iteral]`, `m[ultiline]`, `ne[gative]`, `r[ight-to-left]`, `s[ingleline]`, `s[tarts-]w[ith]`, `timeout=<NUM>`, `w[hole-]l[ine]`, `w[hole-word]`\.

##### `[-y|--display] <DISPLAY_OPTIONS>`

Display of the results\.

[\<DISPLAY_OPTIONS>](OptionValues.md#display_options): `c[ontent]=`[\<CONTENT_DISPLAY>](OptionValues.md#content_display), `co[ntext]=<NUM>`, `b [context-before]=<NUM>`, `a [context-after]=<NUM>`, `c[ount]`, `c[reation-]t[ime]`, `indent=<INDENT>`, `l[ine-number]`, `m[odified-]t[ime]`, `no-align`, `p[ath]=`[\<PATH_DISPLAY>](OptionValues.md#path_display), `s[ize]`, `s[eparator]=<SEPARATOR>`, `su[mmary]`, `trim-line`\.

##### `[-d|--dry-run]`

Display which files or directories should be copied/deleted but do not actually copy/delete any file or directory\.

##### `[--encoding] <ENCODING>`

Encoding to use when a file does not contain BOM\. Default encoding is UTF\-8\.

##### `[-e|--extension] EXT1[,EXT2] [<EXTENSION_OPTIONS>]`

A filter for file extensions \(case\-insensitive by default\)\.

[\<EXTENSION_OPTIONS>](OptionValues.md#extension_options): `c[ase-]s[ensitive]`, `c[ulture-]i[nvariant]`, `f[rom-file]`, `l[ist-]s[eparator]`, `l[iteral]`, `ne[gative]`, `timeout=<NUM>`\.

##### `[-h|--help]`

Show command line help\.

##### `[-t|--highlight] <HIGHLIGHT>`

Parts of the output to highlight\.

[\<HIGHLIGHT>](OptionValues.md#highlight): `n[one]`, `m[atch]`, `e[mpty-]m[atch]`, `e[mpty]`, `b[oundary]`, `t[ab]`, `c[arriage-]r[eturn]`, `l[ine]f[eed]`, `newline`, `space`\.

##### `[-i|--include-directory] <PATTERN> [<PATTERN_OPTIONS>]`

Regular expression for a directory name\.

##### `[-m|--max-count] <MAX_OPTIONS>`

Stop searching after specified number is reached\.

[\<MAX_OPTIONS>](OptionValues.md#max_options): `<NUM>`, `m[atches]=<NUM>`\.

##### `[-n|--name] <PATTERN> [<PATTERN_OPTIONS>]`

Regular expression for file or directory name\.

##### `[--no-recurse]`

Do not search subdirectories\.

##### `[-o|--output] <PATH> [<OUTPUT_OPTIONS>]`

Path to a file that should store output\.

##### `[--paths] <PATH>`

Path to one or more files and/or directories that should be searched\.

##### `[--paths-from] <FILE_PATH>`

Read the list of paths to search from a file\. Paths should be separated by newlines\.

##### `[--progress]`

Display dot \(\.\) for every hundredth searched file or directory\.

##### `[-p|--properties] <FILE_PROPERTIES>`

A filter for file properties\.

[\<FILE_PROPERTIES>](OptionValues.md#file_properties): `c[reation-]t[ime]`, `m[odified-]t[ime]`, `s[ize]=<NUM>`\.

##### `[--second] <DIRECTORY_PATH>`

A directory to be synchronized with a first directory\. It can be also specified as a last unnamed parameter\.

##### `[-s|--sort] <SORT_OPTIONS>`

Sort matched files and directories\.

[\<SORT_OPTIONS>](OptionValues.md#sort_options): `a[scending]`, `c[reation-]t[ime]`, `ci [culture-invariant]`, `d[escending]`, `m[ax-count]=<NUM>`, `m[odified-]t[ime]`, `n[ame]`, `s[ize]`\.

##### `[-v|--verbosity] <VERBOSITY>`

The amount of information to display in the log\.

[\<VERBOSITY>](OptionValues.md#verbosity): `q[uiet]`, `m[inimal]`, `n[ormal]`, `d[etailed]`, `di[agnostic]`\.


*\(Generated with [DotMarkdown](http://github.com/JosefPihrt/DotMarkdown)\)*