import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Navbar from './components/Navbar';
import Home from './pages/Home';
import CandidateDashboard from './pages/CandidateDashboard';
import GapAnalysis from './pages/GapAnalysis';
import JobBoard from './pages/JobBoard';
import HRDashboard from './pages/HRDashboard';
import './index.css';
import './App.css';

function App() {
  return (
    <Router>
      <div className="app-container">
        <Navbar />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/candidate" element={<CandidateDashboard />} />
            <Route path="/jobs" element={<JobBoard />} />
            <Route path="/hr" element={<HRDashboard />} />
            <Route path="/analysis/:jobId" element={<GapAnalysis />} />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;
