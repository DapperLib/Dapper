# Dapper - a simple object mapper for .Net

## Overview

A brief guide is available [on github](https://github.com/StackExchange/dapper-dot-net/blob/master/Readme.md)

More examples coming soon on Stack Overflow docs.

Questions on Stack Overflow should be tagged [`dapper`](http://stackoverflow.com/questions/tagged/dapper)

## Installation

From NuGet:

    Install-Package Dapper

or

    Install-Package Dapper.StrongName

Note: to get the latest pre-release build, add ` -Pre` to the end of the command.

## Release  Notes

### 1.50.4

- Added back missing .NET Standard functionality (restored in `netstandard2.0`)
- Bumped `SqlClient` dependency to 4.4.0 (to help propagate the newer client)

### 1.50.2

- fix issue 569 (`in` expansions using ODBC pseudo-positional arguments)

### 1.50.1

- change to how `string_split` is used for `InListStringSplitCount`

### 1.50.0

- no changes; stable release

### 1.50.0-rc3

- Updated for .Net Core RTM package dependencies

### 1.50.0-rc2b

- new `InListStringSplitCount` global setting; if set (non-negative), `in @foo` expansions (of at least the specified size) of primitive types (`int`, `tinyint`, `smallint`, `bigint`) are implemented via the SQL Server 2016 (compat level 130) `STRING_SPLIT` function
- fix for incorrect conversions in `GridReader` (#254)

### 1.50.0-rc2 / 1.50.0-rc2a

- packaging for .NET Core rc2

### 1.50-beta9

- fix for `PadListExpansions` to work correctly with `not in` scenarios; now uses last non-null value instead of `null`; if none available, don't pad
- fix problems with single-result/single-row not being supported by all providers (basically: sqlite, #466)
- fix problems with enums - nulls (#467) and primitive values (#468)
- add support for C# 6 get-only properties (#473)
- add support for various xml types (#427)

### 1.50-beta8

- addition of `GetRowParser<T>` extension method on `IDataReader` API - allows manual construction of discriminated unions, etc
- addition of `Settings.PadListExpansions` - reduces query-plan saturation by padding list expansions with `null` values (opt-in, because on some DB configurations this could change the meaning) *(note: bad choice of `null` revised in 1.50-beta9)*
- addition of `Settings.ApplyNullValues` - assigns (rather than ignores) `null` values when possible
- fix for #461 - ensure type-handlers work for constructor-based initialization
- fix for #455 - make the `LookupDbType` method available again

### 1.50-beta7

- addition of `GetRowParser(Type)` (and refactor the backing store for readers to suit)
- column hash should consider type, not just name

### 1.50-beta6

- fix for issue #424 - defensive `SqlDataRecord` handling

### 1.50-beta5

- Add "single", "first", "single or default" to complement the "first or default" options from 1.50-beta4
- Use single-row/single-result when possible
- Fix for proxy-generator (issue #361)

### 1.50-beta4

- Add `QueryFirstOrDefault` / `ReadFirstOrDefault` methods that optimize the single-row scenario
- remove some legacy `dynamic` usage from the async API
- make `DynamicTypeMap` public again (error during core-clr migration)
- use `Hashtable` again on core-clr 

### 1.50-beta3

- Core CLR support: add explicit `dnx451` support in addition to `dotnet5.4` (aka `netstandard1.4`)

### 1.50-beta2

- Core CLR now targets rc1 / 23516
- various Core CLR fixes
- code cleanup and C# 6 usage (assorted)

### 1.50-beta1

- split `SqlMapper.cs` as it was becoming too unmaintainable; NuGet is now the only supported deployment channel
- remove down-level C# requirements, as "drop in the file" is no longer the expected usage
- `SqlMapper.Settings` added; provides high-level global configuration; initially `CommandTimeout` (@Irrational86)
- improve error message if an array is used as a parameter in an invalid context
- add `Type[]` support for `GridReader.Read` scenarios (@NikolayGlynchak)
- support for custom type-maps in collection parameters (@gjsduarte)
- fix incorrect cast in `QueryAsync<T>` (@phnx47, #346)
- fix incorrect null handling re `UdtTypeName` (@perliedman)
- support for `SqlDataRecord` (@sqmgh)
- allow `DbString` default for `IsAnsi` to be specified (@kppullin)
- provide `TypeMapProvider` with lazy func-based initialization (@garyhuntddn)
- core-clr updated to beta-8 and various cleanups/fixes
- built using core-clr build tools


### 1.42

- fix bug with dynamic parameters where `.Get<T>` is called before the command is executed

### 1.41-beta5

- core-clr packaging build and workarounds
- fix bug with literal `{=val}` boolean replacements

### 1.41-beta4

- core-clr packaging build
- improve mapping to enum members (@BrianJolly)

### 1.41-beta

- core-clr packaging build

### 1.41-alpha

- introduces dnx (core-clr) experimental changes
- adds `SqlBuilder` project
- improve error message when incorrectly accessing parameter values

### 1.40

- workaround for broken `GetValues()` on Mono; add `AsList()`

### 1.39

- fix case on SQL CLR types; grid-reader should respect no-cache flags; make parameter inclusion case-insensitive

### 1.38

- specify constructor explicitly; allow value-type parameters (albeit: boxed)

### 1.37

- Reuse StringBuilder instances when possible (list parameters in particular)

### 1.36

- Fix Issue #192 (expanded parameter naming glitch) and Issue #178 (execute reader now wraps the command/reader pair, to extend the command lifetime; note that the underlying command/reader are available by casting to `IWrappedDataReader`)

### 1.35

- Fix Issue #151 (Execute should work with `ExpandoObject` etc); Fix Issue #182 (better support for db-type when using `object` values);
- output expressions / callbacks in dynamic args (via Derek); arbitrary number of types in multi-mapping (via James Holwell);
- fix `DbString`/Oracle bug (via Mauro Cerutti); new support for **named positional arguments**

### 1.34

- Support for `SqlHierarchyId` (core)

### 1.33

- Support for `SqlGeometry` (core) and `DbGeometry` (EF)

### 1.32

- Support for `SqlGeography` in core library

### 1.31

- Fix issue with error message when there is a column/type mismatch

### 1.30

- Better async cancellation

### 1.29

- Make underscore name matching optional (opt-in) - this can be a breaking change for some people

### 1.28

- Much better numeric type conversion; fix for large oracle strings; map `Foo_Bar` to `FooBar` (etc); `ExecuteScalar` added; stability fixes

### 1.27

- Fixes for type-handler parse; ensure type-handlers get last dibs on configuring parameters

### 1.26

- New type handler API for extension support

### 1.25

- Command recycling and disposing during pipelined async multi-exec; enable pipeline (via sync-over-async) for sync API"
