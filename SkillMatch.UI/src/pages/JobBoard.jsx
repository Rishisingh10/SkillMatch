import { useState, useEffect, useRef } from 'react';
import { Briefcase, UploadCloud, CheckCircle2, ChevronRight, X } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

const JobBoard = () => {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  const [selectedJob, setSelectedJob] = useState(null);
  const [file, setFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState(null);
  
  const navigate = useNavigate();
  const fileInputRef = useRef(null);

  useEffect(() => {
    fetchJobs();
  }, []);

  const fetchJobs = async () => {
    try {
      const res = await fetch('http://localhost:5256/api/Candidate/jobs');
      if (!res.ok) throw new Error('Failed to fetch jobs');
      const data = await res.json();
      setJobs(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleApplyClick = (job) => {
    setSelectedJob(job);
    setFile(null);
    setUploadError(null);
  };

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
    }
  };

  const handleUpload = async () => {
    if (!file || !selectedJob) return;
    setUploading(true);
    setUploadError(null);
    
    const formData = new FormData();
    formData.append('file', file);
    formData.append('targetJobId', selectedJob.id);
    
    try {
      // Assuming Candidate ID 1 for testing
      const response = await fetch('http://localhost:5256/api/Candidate/1/resume/upload', {
        method: 'POST',
        body: formData,
      });
      
      if (!response.ok) {
        throw new Error(await response.text() || 'Failed to upload resume');
      }
      
      const data = await response.json();
      navigate(`/analysis/${data.jobId}`);
    } catch (err) {
      setUploadError(err.message);
      setUploading(false);
    }
  };

  return (
    <div className="container" style={{ padding: '2rem 0' }}>
      <div className="animate-fade-up">
        <h2>Job Board</h2>
        <p>Browse open positions and apply instantly. AI will analyze your fit.</p>
        
        {loading && <p>Loading jobs...</p>}
        {error && <div style={{ color: 'var(--accent-red)' }}>Error: {error}</div>}
        
        {!loading && jobs.length === 0 && (
          <div className="solid-border hard-shadow" style={{ padding: '2rem', textAlign: 'center' }}>
            <p>No active jobs found. Check back later!</p>
          </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '1.5rem', marginTop: '2rem' }}>
          {jobs.map(job => (
            <div key={job.id} className="solid-border hard-shadow" style={{ padding: '1.5rem', display: 'flex', flexDirection: 'column' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginBottom: '0.5rem' }}>
                <Briefcase size={20} color="var(--primary)" />
                <h3 style={{ margin: 0, fontSize: '1.25rem' }}>{job.title}</h3>
              </div>
              
              <p style={{ fontSize: '0.9rem', color: 'var(--text-muted)', flex: 1 }}>{job.description}</p>
              
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginBottom: '1.5rem' }}>
                {job.skills.slice(0, 3).map(skill => (
                  <span key={skill} style={{ background: 'var(--bg-light)', border: '1px solid var(--border-color)', padding: '0.2rem 0.5rem', fontSize: '0.75rem', fontWeight: 'bold' }}>
                    {skill}
                  </span>
                ))}
                {job.skills.length > 3 && <span style={{ fontSize: '0.75rem', alignSelf: 'center' }}>+{job.skills.length - 3} more</span>}
              </div>

              <button className="retro-button solid" onClick={() => handleApplyClick(job)}>
                APPLY NOW <ChevronRight size={16} />
              </button>
            </div>
          ))}
        </div>
      </div>

      {/* Upload Modal */}
      {selectedJob && (
        <div style={{
          position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, 
          backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 100, 
          display: 'flex', alignItems: 'center', justifyContent: 'center'
        }}>
          <div className="solid-border hard-shadow" style={{ backgroundColor: 'var(--bg-light)', padding: '2rem', width: '90%', maxWidth: '500px', position: 'relative' }}>
            <button onClick={() => setSelectedJob(null)} style={{ position: 'absolute', top: '1rem', right: '1rem', background: 'none', border: 'none', cursor: 'pointer' }}>
              <X size={24} />
            </button>
            
            <h3 style={{ marginTop: 0 }}>Apply: {selectedJob.title}</h3>
            <p style={{ fontSize: '0.9rem', marginBottom: '1.5rem' }}>Upload your resume to apply. AI will automatically evaluate your fit for this role.</p>
            
            <div style={{
              border: '2px dashed var(--border-color)',
              padding: '2rem',
              textAlign: 'center',
              backgroundColor: '#fff',
              cursor: 'pointer',
              marginBottom: '1rem'
            }} onClick={() => fileInputRef.current.click()}>
              <input type="file" ref={fileInputRef} accept=".pdf,.docx" style={{ display: 'none' }} onChange={handleFileChange} />
              <UploadCloud size={32} color="var(--text-muted)" style={{ margin: '0 auto 1rem auto' }} />
              <p style={{ margin: 0, fontWeight: 500 }}>
                {file ? file.name : 'Click to select a PDF or DOCX file'}
              </p>
            </div>

            <button className="retro-button solid" style={{ width: '100%', justifyContent: 'center' }} onClick={handleUpload} disabled={!file || uploading}>
              {uploading ? 'ANALYZING...' : 'SUBMIT APPLICATION'}
            </button>
            
            {uploadError && <div style={{ marginTop: '1rem', color: 'var(--accent-red)', fontSize: '0.9rem' }}>{uploadError}</div>}
          </div>
        </div>
      )}
    </div>
  );
};

export default JobBoard;
