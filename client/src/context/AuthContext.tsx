import { createContext, useContext, useState, useEffect } from "react";
import { apiGet, apiPost, setAccessToken } from "../lib/api";

export interface User {
  id: string;
  email: string;
  role: string;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  signIn: (email: string, password: string) => Promise<void>;
  signOut: () => Promise<void>;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  // Try to silently refresh token on app initialize
  useEffect(() => {
    let active = true;

    async function initAuth() {
      try {
        const data = await apiPost<{ accessToken: string }, Record<string, never>>("/api/refresh", {});
        if (data?.accessToken) {
          setAccessToken(data.accessToken);
          const authData = await apiGet<{ user: User }>("/api/me");
          if (active) {
            setUser(authData.user);
          }
        }
      } catch (err) {
        // Ignored, user is just guest
        console.log("Not logged in or session expired on initialize", err);
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    initAuth();

    return () => {
      active = false;
    };
  }, []);

  const signIn = async (email: string, password: string) => {
    setLoading(true);
    try {
      const response = await apiPost<{ accessToken: string; user: User }, any>("/api/sign-in", {
        email,
        password,
      });

      if (response?.accessToken) {
        setAccessToken(response.accessToken);
        setUser(response.user);
      } else {
        throw new Error("Не вдалося отримати токен доступу");
      }
    } finally {
      setLoading(false);
    }
  };

  const signOut = async () => {
    setLoading(true);
    try {
      await apiPost("/api/logout", {});
    } catch (err) {
      console.error("Помилка під час логауту на сервері", err);
    } finally {
      setAccessToken(null);
      setUser(null);
      setLoading(false);
    }
  };

  const isAuthenticated = !!user;

  return (
    <AuthContext.Provider value={{ user, loading, signIn, signOut, isAuthenticated }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
