# [mahmoudgamal.dev](https://mahmoudgamal.dev)

The personal website and portfolio of Mahmoud Gamal, a .NET technical lead.

## Status

The project is in early setup. There is no site code yet; this README is the first file in the repository.

## Workflow

Work is tracked in Linear, in the **MAH** team. Issues labelled `claude` are picked up automatically by a scheduled Claude worker, which implements the change on a branch named after the issue and opens a **draft pull request** against `main` for review. Pull request titles and commit messages start with the issue identifier (for example, `MAH-12: Add contact form`), and each PR links back to its Linear issue.
