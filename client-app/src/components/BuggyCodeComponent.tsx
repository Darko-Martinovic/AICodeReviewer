import React, { useState, useEffect } from "react";
import styles from "./BuggyCodeComponent.module.css";

// INTENTIONALLY BUGGY REACT COMPONENT FOR TESTING CODE REVIEW
// This component contains multiple critical issues

interface User {
  id: number;
  name: string;
  email: string;
  password?: string; // BUG: Password should never be in frontend state
  creditCard?: string; // BUG: Sensitive data in frontend
}

const BuggyCodeComponent: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // BUG: Missing cleanup in useEffect
  useEffect(() => {
    const interval = setInterval(() => {
      console.log("Background task running...");
      // BUG: Memory leak - interval never cleared
    }, 1000);

    // BUG: Missing return () => clearInterval(interval);
  }, []);

  // BUG: XSS vulnerability - dangerous HTML insertion
  const renderUserComment = (comment: string) => {
    return <div dangerouslySetInnerHTML={{ __html: comment }} />;
  };

  // BUG: Insecure API call with hardcoded credentials
  const fetchUserData = async () => {
    setIsLoading(true);

    try {
      // BUG: Hardcoded API key in frontend code
      const apiKey = "sk-1234567890abcdef-NEVER-PUT-THIS-IN-FRONTEND";

      // BUG: HTTP instead of HTTPS for sensitive data
      const response = await fetch("http://api.example.com/users", {
        headers: {
          Authorization: `Bearer ${apiKey}`,
          "X-Admin-Password": "admin123", // BUG: Hardcoded admin password
        },
      });

      if (!response.ok) {
        // BUG: Exposing detailed error information to console
        console.error("API Error:", response.status, response.statusText);
        throw new Error(`API call failed: ${response.status}`);
      }

      const userData = await response.json();

      // BUG: No data validation - trusting API response completely
      setUsers(userData.users);

      // BUG: Storing sensitive data in localStorage
      localStorage.setItem(
        "userPasswords",
        JSON.stringify(userData.users.map((u: User) => u.password))
      );
      localStorage.setItem("apiKey", apiKey);
    } catch (error) {
      // BUG: Exposing full error details to user
      alert(`Error fetching user data: ${error}`);
    } finally {
      setIsLoading(false);
    }
  };

  // BUG: SQL injection vulnerability in search
  const searchUsers = async (searchTerm: string) => {
    // BUG: Constructing SQL-like query on frontend (conceptual error)
    const query = `SELECT * FROM users WHERE name LIKE '%${searchTerm}%'`;
    console.log("Executing query:", query); // BUG: Logging sensitive query

    // BUG: No input sanitization
    const filteredUsers = users.filter(
      (user) =>
        user.name.includes(searchTerm) || user.email.includes(searchTerm)
    );

    setUsers(filteredUsers);
  };

  // BUG: Infinite loop potential
  const processUserData = () => {
    let processed = 0;
    while (processed < users.length) {
      const user = users[processed];

      // BUG: Condition never changes if user is invalid
      if (user && user.name) {
        processed++;
      }
      // Missing else clause could cause infinite loop
    }
  };

  // BUG: Race condition with state updates
  const updateUserConcurrently = async (userId: number) => {
    // BUG: Multiple async operations modifying same state
    Promise.all([
      fetch(`/api/users/${userId}/update1`),
      fetch(`/api/users/${userId}/update2`),
      fetch(`/api/users/${userId}/update3`),
    ]).then((responses) => {
      // BUG: No error handling for individual promises
      responses.forEach((response, index) => {
        setUsers((prev) => {
          // BUG: Race condition - multiple setState calls
          const updated = [...prev];
          updated[index] = { ...updated[index], name: `Updated-${index}` };
          return updated;
        });
      });
    });
  };

  // BUG: Memory leak with large object creation
  const handleLargeDataOperation = () => {
    const largeArray = new Array(1000000).fill(0).map((_, index) => ({
      id: index,
      data: new Array(1000).fill(`data-${index}`).join(""),
      metadata: {
        timestamp: new Date(),
        randomData: Math.random().toString(36),
      },
    }));

    // BUG: Storing large data in state unnecessarily
    setUsers((prev) => [...prev, ...largeArray]);
  };

  // BUG: Unsafe eval usage
  const executeUserScript = (userScript: string) => {
    try {
      // CRITICAL BUG: Never use eval with user input!
      eval(userScript);
    } catch (error) {
      console.error("Script execution failed:", error);
    }
  };

  // BUG: Prototype pollution vulnerability
  const mergeUserSettings = (userSettings: any) => {
    const defaultSettings = {};

    // BUG: Unsafe object merge - prototype pollution
    for (const key in userSettings) {
      defaultSettings[key] = userSettings[key];
    }

    return defaultSettings;
  };

  // BUG: No error boundary - component can crash entire app
  return (
    <div className={styles.container}>
      <h1>User Management Dashboard</h1>

      {/* BUG: No loading state handling */}
      {isLoading && <div>Loading...</div>}

      <button onClick={fetchUserData}>Fetch User Data</button>

      <button onClick={() => handleLargeDataOperation()}>
        Process Large Dataset (Memory Leak)
      </button>

      {/* BUG: No key prop in list rendering */}
      {users.map((user) => (
        <div className={styles.userCard}>
          <h3>{user.name}</h3>
          <p>Email: {user.email}</p>

          {/* BUG: Displaying sensitive information */}
          {user.password && <p>Password: {user.password}</p>}
          {user.creditCard && <p>Credit Card: {user.creditCard}</p>}

          {/* BUG: XSS vulnerability */}
          {renderUserComment(user.name)}

          <button onClick={() => updateUserConcurrently(user.id)}>
            Update User (Race Condition)
          </button>

          <button
            onClick={() => executeUserScript(`alert('Hello ${user.name}')`)}
          >
            Execute Script (Eval)
          </button>
        </div>
      ))}

      {/* BUG: Inline event handlers with no validation */}
      <input
        type="text"
        placeholder="Search users..."
        onChange={(e) => searchUsers(e.target.value)}
        onKeyPress={(e) => {
          if (e.key === "Enter") {
            // BUG: No input validation
            executeUserScript(e.currentTarget.value);
          }
        }}
      />

      {/* BUG: Missing error handling for form submission */}
      <form
        onSubmit={(e) => {
          e.preventDefault();
          const formData = new FormData(e.currentTarget);
          const userData = Object.fromEntries(formData);

          // BUG: No validation before adding user
          setUsers((prev) => [...prev, userData as any]);
        }}
      >
        <input name="name" placeholder="Name" required />
        <input name="email" type="email" placeholder="Email" required />
        <input
          name="password"
          type="password"
          placeholder="Password"
          required
        />
        <input name="creditCard" placeholder="Credit Card" />
        <button type="submit">Add User</button>
      </form>
    </div>
  );
};

export default BuggyCodeComponent;
