#!/usr/bin/env bash

set -e

echo "🧪 Running F# Assistant Agent Test Suite"
echo "========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Track overall status
ALL_PASSED=true

# Function to run tests for a project
run_test_project() {
    local project_name=$1
    local project_path=$2

    echo "📦 Testing: $project_name"
    echo "---"

    if dotnet test "$project_path" --logger "console;verbosity=normal"; then
        echo -e "${GREEN}✓ $project_name: PASSED${NC}"
        echo ""
    else
        echo -e "${RED}✗ $project_name: FAILED${NC}"
        echo ""
        ALL_PASSED=false
    fi
}

# Run all test projects
run_test_project "Agent.Core.Tests" "src/Agent.Core.Tests/Agent.Core.Tests.fsproj"
run_test_project "Agent.Storage.Tests" "src/Agent.Storage.Tests/Agent.Storage.Tests.fsproj"
run_test_project "Agent.Executor.Tests" "src/Agent.Executor.Tests/Agent.Executor.Tests.fsproj"
run_test_project "Agent.Web.Tests" "src/Agent.Web.Tests/Agent.Web.Tests.fsproj"

# Summary
echo "========================================="
if [ "$ALL_PASSED" = true ]; then
    echo -e "${GREEN}✓ All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}✗ Some tests failed${NC}"
    exit 1
fi
