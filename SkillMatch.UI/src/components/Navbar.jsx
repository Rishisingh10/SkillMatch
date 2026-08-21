import { Link, useLocation } from 'react-router-dom';
import { Cpu } from 'lucide-react';

const Navbar = () => {
  const location = useLocation();

  return (
    <nav className="navbar">
      <div className="container nav-content">
        <Link to="/" className="nav-brand">
          <Cpu color="var(--text-dark)" size={32} />
          <span>SkillMatchAI</span>
        </Link>
        <div className="nav-links">
          <Link to="/" className={`nav-link ${location.pathname === '/' ? 'active' : ''}`}>
            VISION
          </Link>
          <Link to="/hr" className={`nav-link ${location.pathname.includes('/hr') ? 'active' : ''}`}>
            HR PORTAL
          </Link>
          <Link to="/jobs" className={`nav-link ${location.pathname.includes('/jobs') ? 'active' : ''}`}>
            JOB BOARD
          </Link>
          <Link to="/candidate" className={`nav-link ${location.pathname.includes('/candidate') ? 'active' : ''}`}>
            CANDIDATE DASHBOARD
          </Link>
        </div>
      </div>
    </nav>
  );
};

export default Navbar;
