# Postman Collection Update - Cache Management

## 📋 **Summary**

The Postman collection has been updated to **version 1.1.0** to include the new cache management endpoints that were added to the AI Code Reviewer API.

## 🆕 **What's New**

### **Cache Management Section**

A new folder "Cache Management" has been added with 5 endpoints:

#### 1. **Check Commit Cache Status**

- **Method**: `GET`
- **URL**: `/api/cache/commit/{commitSha}/exists`
- **Purpose**: Check if a commit review is cached
- **Returns**: `true` if cached, `false` otherwise
- **Variables Used**: `{{commitSha}}`

#### 2. **Check Pull Request Cache Status**

- **Method**: `GET`
- **URL**: `/api/cache/pullrequest/{prNumber}/exists`
- **Purpose**: Check if a pull request review is cached
- **Returns**: `true` if cached, `false` otherwise
- **Variables Used**: `{{prNumber}}`

#### 3. **Clear All Cache**

- **Method**: `DELETE`
- **URL**: `/api/cache/clear`
- **Purpose**: Clear all cached reviews (commits and PRs)
- **Returns**: Success message
- **⚠️ Warning**: Use with caution - removes ALL cached data

#### 4. **Clear Specific Commit Cache**

- **Method**: `DELETE`
- **URL**: `/api/cache/commit/{commitSha}`
- **Purpose**: Clear cached review for a specific commit
- **Returns**: Success message
- **Variables Used**: `{{commitSha}}`

#### 5. **Clear Specific Pull Request Cache**

- **Method**: `DELETE`
- **URL**: `/api/cache/pullrequest/{prNumber}`
- **Purpose**: Clear cached review for a specific PR
- **Returns**: Success message
- **Variables Used**: `{{prNumber}}`

## 📁 **Files Updated**

### ✅ **Updated**

- **`AICodeReviewer_API_Collection.postman_collection.json`**
  - Added Cache Management folder with 5 new endpoints
  - Updated version from 1.0.0 → 1.1.0
  - Updated description to mention cache management

### ✅ **No Changes Needed**

- **`AICodeReviewer_Environment.postman_environment.json`**
  - All existing variables work with new cache endpoints
  - No new environment variables required

## 🧪 **How to Test Cache Functionality**

### **Testing Cache with Commits:**

1. **First**: Run "Review Specific Commit" → Should take ~23 seconds (fresh review)
2. **Check**: Run "Check Commit Cache Status" → Should return `true`
3. **Repeat**: Run "Review Specific Commit" again → Should be nearly instant (cached)
4. **Clear**: Run "Clear Specific Commit Cache" → Clears that commit
5. **Verify**: Run "Check Commit Cache Status" → Should return `false`

### **Testing Cache with Pull Requests:**

1. **First**: Run "Review Pull Request" → Should take ~3 minutes (fresh review)
2. **Check**: Run "Check Pull Request Cache Status" → Should return `true`
3. **Repeat**: Run "Review Pull Request" again → Should be nearly instant (cached)
4. **Clear**: Run "Clear Specific Pull Request Cache" → Clears that PR
5. **Verify**: Run "Check Pull Request Cache Status" → Should return `false`

### **Cache Management:**

- Use "Clear All Cache" to reset everything during testing
- Cache expires automatically after 24 hours (configurable)

## 🔧 **Import Instructions**

1. **Import the updated collection** in Postman
2. **Keep your existing environment** - no changes needed
3. **Set variables** as usual:
   - `commitSha`: Get from "Get Recent Commits" response
   - `prNumber`: Set to any PR number you want to test

## 📊 **Expected Performance Improvements**

- **First Review**: Same timing as before (23s commits, 3m PRs)
- **Cached Reviews**: Nearly instant (< 1 second)
- **Cache Hit Rate**: High for repeated testing of same commits/PRs
- **Storage**: In-memory cache, no disk space used

## 🔍 **API Responses**

### **Cache Status Endpoints**

```json
// GET /api/cache/commit/{sha}/exists
true  // or false

// GET /api/cache/pullrequest/{number}/exists
true  // or false
```

### **Clear Cache Endpoints**

```json
// DELETE /api/cache/clear
"All cached reviews cleared successfully"

// DELETE /api/cache/commit/{sha}
"Cached review for commit abc123... cleared successfully"

// DELETE /api/cache/pullrequest/{number}
"Cached review for PR #42 cleared successfully"
```

The updated collection is now ready for testing the new caching functionality! 🎉
