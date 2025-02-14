# lip pack

## Usage

```shell
lip pack <output-path>
```

## Description

Create a archive from the current directory, containing all files to place specified in the `tooth.json` file.

## Options

- `--dry-run`

  Do not actually create the archive.

- `--ignore-scripts`

  Do not run any scripts during packaging.

- `--archive-format <format>`

  The format of the archive to create.

  Valid formats are `zip`, `tar`, `tgz` and `tar.gz`. Defaults to `zip`.
