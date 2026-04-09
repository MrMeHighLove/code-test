import { createContext, useState } from 'react';
import type { ReactNode } from 'react';
import { login as apiLogin, register as apiRegister, logout as apiLogout } from '../api/auth';

export interface AuthContextType {
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  register: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(
    () => localStorage.getItem('isAuthenticated') === 'true'
  );

  const login = async (username: string, password: string) => {
    await apiLogin(username, password);
    localStorage.setItem('isAuthenticated', 'true');
    setIsAuthenticated(true);
  };

  const register = async (username: string, password: string) => {
    await apiRegister(username, password);
  };

  const logout = async () => {
    await apiLogout();
    localStorage.removeItem('isAuthenticated');
    setIsAuthenticated(false);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export { AuthContext };
