import { useState } from 'react';
import type { ComponentProps } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { AUTH_PAGE, ROUTES } from '../constants/app';
import { useAuth } from '../context/useAuth';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit: NonNullable<ComponentProps<'form'>['onSubmit']> = async e => {
    e.preventDefault();
    setError('');
    try {
      await login(username, password);
      navigate(ROUTES.products);
    } catch {
      setError(AUTH_PAGE.invalidCredentials);
    }
  };

  return (
    <div className="auth-container">
      <h1>{AUTH_PAGE.loginTitle}</h1>
      {error && <p className="error">{error}</p>}
      <form onSubmit={handleSubmit}>
        <input
          type="text"
          placeholder={AUTH_PAGE.usernamePlaceholder}
          value={username}
          onChange={e => setUsername(e.target.value)}
          required
        />
        <input
          type="password"
          placeholder={AUTH_PAGE.passwordPlaceholder}
          value={password}
          onChange={e => setPassword(e.target.value)}
          required
        />
        <button type="submit">{AUTH_PAGE.loginButton}</button>
      </form>
      <p>{AUTH_PAGE.loginPrompt} <Link to={ROUTES.register}>{AUTH_PAGE.registerButton}</Link></p>
    </div>
  );
}
