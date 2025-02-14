# lip update

## Usage

```shell
lip update <package-spec>...
```

## Description

Attempt to update the specified packages to the specified versions.

## Options

- `--dry-run`

  Do not actually update any packages. Be aware that files will still be downloaded and cached.

- `--ignore-scripts`

  Do not run any scripts during updating.

- `--no-dependencies`

  Do not update dependencies.

- `--save`

  Save the updated packages to `tooth.json`. Only apply to default variant.
