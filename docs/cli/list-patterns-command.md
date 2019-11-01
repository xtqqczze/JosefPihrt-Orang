# `orang list-patterns`

Lists all basic patterns that will match specified character\.

## Synopsis

```
orang list-patterns [<CHAR>]
[   --char-group]
[-o|--options]    <REGEX_OPTIONS>
```

## Arguments

**`<CHAR>`**

Character or a decimal number that represents the character\. For a number literal use escape like \\1\.

## Options

**`[--char-group]`**

Treat character as if it is in the character group\.

**`[-o|--options] <REGEX_OPTIONS>`**

Regex options that should be used\. Relevant values are \[e\]cma\-\[s\]cript or \[i\]gnore\-case\. Allowed values are \[c\]ompiled, \[c\]ulture\-\[i\]nvariant, \[e\]cma\-\[s\]cript, \[n\] explicit\-capture, \[i\]gnore\-case, \[x\] ignore\-pattern\-whitespace, \[m\]ultiline, \[r\]ight\-to\-left and \[s\]ingleline\.


*\(Generated with [DotMarkdown](http://github.com/JosefPihrt/DotMarkdown)\)*