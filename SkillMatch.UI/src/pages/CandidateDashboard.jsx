import { useState } from 'react';
import { UploadCloud, CheckCircle2, User, Book, MapPin, Phone } from 'lucide-react';

const CandidateDashboard = () => {
  const [file, setFile] = useState(null);
  const [targetJobTitle, setTargetJobTitle] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);

  const handleFileChange = (e) => {
    if (e.target.files && e.target.files[0]) {
      setFile(e.target.files[0]);
    }
  };

  const handleUpload = async () => {
    if (!file) return;
    setLoading(true);
    setError(null);
    
    const formData = new FormData();
    formData.append('file', file);
    if (targetJobTitle) {
      formData.append('targetJobTitle', targetJobTitle);
    }
    
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
      setResult(data);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container" style={{ padding: '2rem 0' }}>
      <div className="animate-fade-up">
        <h2>Candidate Portal</h2>
        <p>Upload your resume to have our AI extract your skills and experience automatically.</p>
        
        <div className="grid-2" style={{ marginTop: '2rem' }}>
          {/* Upload Section */}
          <div className="solid-border hard-shadow" style={{ padding: '2rem', display: 'flex', flexDirection: 'column', gap: '1.5rem' }}>
            <h3><UploadCloud size={24} style={{ marginRight: '0.5rem', verticalAlign: 'middle', color: 'var(--primary)' }}/> Upload Resume</h3>
            
            <div>
              <label style={{ fontWeight: 600, display: 'block', marginBottom: '0.5rem' }}>Target Role / Job Title</label>
              <input 
                type="text" 
                className="solid-border"
                style={{ width: '100%', padding: '0.75rem', fontFamily: 'var(--font-body)', fontSize: '1rem' }} 
                placeholder="e.g. Data Scientist, Frontend Developer..." 
                value={targetJobTitle}
                onChange={(e) => setTargetJobTitle(e.target.value)}
              />
            </div>
            
            <div style={{
              border: '2px dashed var(--border-color)',
              padding: '3rem 2rem',
              textAlign: 'center',
              backgroundColor: 'var(--bg-light)',
              transition: 'var(--transition)',
              cursor: 'pointer'
            }} onClick={() => document.getElementById('resume-upload').click()}>
              <input type="file" id="resume-upload" accept=".pdf,.docx" style={{ display: 'none' }} onChange={handleFileChange} />
              
              <UploadCloud size={48} color="var(--text-muted)" style={{ margin: '0 auto 1rem auto' }} />
              <p style={{ margin: 0, fontWeight: 500 }}>
                {file ? file.name : 'Click to select a PDF or DOCX file'}
              </p>
              {file && <p style={{ fontSize: '0.85rem', marginTop: '0.5rem' }}>{(file.size / 1024).toFixed(2)} KB</p>}
            </div>
            
            <button className="retro-button solid" style={{ width: '100%', justifyContent: 'center' }} onClick={handleUpload} disabled={!file || loading}>
              {loading ? 'Processing with AI...' : 'Upload & Parse Resume'}
            </button>
            
            {error && <div style={{ padding: '1rem', background: 'rgba(239, 68, 68, 0.1)', color: '#ef4444', borderRadius: '8px', fontSize: '0.9rem' }}>{error}</div>}
          </div>
          
          {/* Result Section */}
          <div className="solid-border hard-shadow" style={{ padding: '2rem' }}>
            <h3>Extraction Results</h3>
            {!result ? (
              <div style={{ height: '100%', display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--text-dark)' }}>
                Upload a resume to see your parsed profile here.
              </div>
            ) : (
              <div className="animate-fade-up">
                <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', color: '#10b981', marginBottom: '1.5rem', fontWeight: 500 }}>
                  <CheckCircle2 size={20} /> {result.message}
                </div>
                
                <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
                  <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                    <User color="var(--text-muted)" size={18} />
                    <span><strong>Headline:</strong> {result.aiExtractedProfile?.headline || 'N/A'}</span>
                  </div>
                  <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                    <MapPin color="var(--text-muted)" size={18} />
                    <span><strong>Location:</strong> {result.aiExtractedProfile?.location || 'N/A'}</span>
                  </div>
                  <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                    <Phone color="var(--text-muted)" size={18} />
                    <span><strong>Phone:</strong> {result.aiExtractedProfile?.phone || 'N/A'}</span>
                  </div>
                  <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
                    <Book color="var(--text-muted)" size={18} />
                    <span><strong>Education:</strong> {result.aiExtractedProfile?.educationLevel || 'N/A'}</span>
                  </div>
                </div>
                
                <hr style={{ border: 'none', borderTop: '1px solid var(--border-color)', margin: '1.5rem 0' }} />
                
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '1rem' }}>
                  <strong>Extracted Skills:</strong>
                  <span style={{ background: 'var(--accent-red)', color: 'var(--text-light)', padding: '0.1rem 0.6rem', fontSize: '0.9rem', fontWeight: 600 }}>
                    {result.extractedSkillsCount} found
                  </span>
                </div>
                
                <p style={{ fontSize: '0.9rem' }}>
                  <span style={{ color: '#10b981', fontWeight: 600 }}>+{result.newSkillsAddedToProfile}</span> new skills were automatically added to your database profile!
                </p>
                
                {result.jobId && (
                  <div style={{ marginTop: '2rem' }}>
                    <a href={`/analysis/${result.jobId}`} className="retro-button solid" style={{ width: '100%', display: 'block' }}>
                      VIEW GAP ANALYSIS FOR ROLE &rarr;
                    </a>
                  </div>
                )}
                
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default CandidateDashboard;
