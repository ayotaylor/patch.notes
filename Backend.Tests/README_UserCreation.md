# User Creation Test Methods

This file contains methods for creating test users in the `patchnotesdb` MySQL database.

## Methods Available

### 1. `CreateThirtyTestUsers_ShouldSucceed` (Automated Test)
- **Purpose**: Automated unit test that creates 30 test users with realistic data
- **Email Pattern**: `testuser01@patch.notes` through `testuser30@patch.notes`
- **Cleanup**: ✅ **Automatically cleaned up** after test completes
- **Output File**: `test_users.txt` (temporary, for test validation)
- **Usage**: Run with standard test commands - users are automatically cleaned up

### 2. `CreatePermanentTestUsers_ForManualTesting` (Manual Execution)
- **Purpose**: Creates 30 permanent test users for manual testing and development
- **Email Pattern**: `user01@testusers.dev` through `user30@testusers.dev`
- **Cleanup**: ❌ **NOT cleaned up automatically** - users persist in database
- **Output File**: `permanent_test_users.txt` (contains login credentials)
- **Usage**: 
  1. Remove the `Skip` attribute from the test method
  2. Run the test once: `dotnet test --filter "CreatePermanentTestUsers_ForManualTesting"`
  3. Add the `Skip` attribute back to prevent accidental re-runs

### 3. `CleanupPermanentTestUsers_ForManualExecution` (Utility Method)
- **Purpose**: Removes all permanent test users from the database
- **Usage**: 
  1. Remove the `Skip` attribute from the test method
  2. Run: `dotnet test --filter "CleanupPermanentTestUsers_ForManualExecution"`
  3. Add the `Skip` attribute back

## How to Create Permanent Test Users

1. **Edit the test file**:
   ```csharp
   [Fact] // Remove the Skip attribute
   public async Task CreatePermanentTestUsers_ForManualTesting()
   ```

2. **Run the test**:
   ```bash
   dotnet test --filter "CreatePermanentTestUsers_ForManualTesting"
   ```

3. **Check the output**:
   - Users are created in the `patchnotesdb` database
   - Credentials are written to `permanent_test_users.txt`

4. **Re-add the Skip attribute** to prevent accidental re-runs:
   ```csharp
   [Fact(Skip = "Manual execution only - uncomment and run when you need to create permanent test users")]
   ```

## User Credentials Format

The generated `permanent_test_users.txt` file contains:
- **Email**: user01@testusers.dev, user02@testusers.dev, etc.
- **Password**: TestPass123! (same for all users)
- **Full user details**: Names, bios, phone numbers, birth years, etc.

## Database Persistence

- **Test users** (`@patch.notes`): Automatically cleaned up
- **Permanent users** (`@testusers.dev`): Persist until manually deleted
- Both types use the same realistic data generation logic
- All users have confirmed emails and complete profiles