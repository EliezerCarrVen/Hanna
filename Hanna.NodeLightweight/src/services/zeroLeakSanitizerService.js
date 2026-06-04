const { SecretFilterService } = require('./secretFilterService');
class ZeroLeakSanitizerService {
  constructor() { this.secretFilter = new SecretFilterService(); }
  sanitize(input) {
    let text = this.secretFilter.redact(input);
    text = text.replace(/[A-Z]:\\Users\\[^\\\s]+/gi, '[LOCAL_PATH]');
    text = text.replace(/\/home\/[^/\s]+/g, '/home/[USER]');
    text = text.replace(/\b[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}\b/g, '[EMAIL]');
    text = text.replace(/\b(10\.\d{1,3}\.\d{1,3}\.\d{1,3}|192\.168\.\d{1,3}\.\d{1,3}|172\.(1[6-9]|2\d|3[0-1])\.\d{1,3}\.\d{1,3})\b/g, '[PRIVATE_IP]');
    text = text.replace(/\b(user(name)?|uid)[:=][^\s,;]+/gi, '$1=[USER]');
    text = text.replace(/\b(Host|Server|Database|User Id|Password)=([^;]+;?)/gi, '$1=[REDACTED];');
    return text;
  }
}
module.exports = { ZeroLeakSanitizerService };
