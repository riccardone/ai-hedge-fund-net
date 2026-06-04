#!/usr/bin/env bash
# Usage:
#   ./scripts/release.sh          # bumps patch: v0.2.11 → v0.2.12
#   ./scripts/release.sh v0.3.0   # uses the version you supply

set -euo pipefail

# ── Resolve target version ────────────────────────────────────────────────────
if [ -n "${1:-}" ]; then
  NEW_TAG="$1"
  # Normalise: ensure it starts with 'v'
  [[ "$NEW_TAG" == v* ]] || NEW_TAG="v$NEW_TAG"
else
  # Find latest semver tag (ignore pre-release suffixes like -test)
  LATEST=$(git tag --sort=-v:refname | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | head -1)
  if [ -z "$LATEST" ]; then
    echo "ERROR: No existing semver tag found. Please supply a version explicitly."
    echo "  Usage: $0 v0.1.0"
    exit 1
  fi

  # Strip leading 'v' and split into parts
  VERSION="${LATEST#v}"
  MAJOR=$(echo "$VERSION" | cut -d. -f1)
  MINOR=$(echo "$VERSION" | cut -d. -f2)
  PATCH=$(echo "$VERSION" | cut -d. -f3)

  NEW_PATCH=$(( PATCH + 1 ))
  NEW_TAG="v${MAJOR}.${MINOR}.${NEW_PATCH}"
fi

# ── Safety checks ─────────────────────────────────────────────────────────────
if git rev-parse "$NEW_TAG" >/dev/null 2>&1; then
  echo "ERROR: Tag '$NEW_TAG' already exists."
  exit 1
fi

if [ -n "$(git status --porcelain)" ]; then
  echo "ERROR: Working tree is dirty. Commit or stash your changes first."
  exit 1
fi

# ── Tag and push ──────────────────────────────────────────────────────────────
echo "Cutting release $NEW_TAG ..."
git tag -a "$NEW_TAG" -m "Release $NEW_TAG"
git push origin "$NEW_TAG"
echo "Done — GitHub Actions will now build, test, and publish $NEW_TAG."
