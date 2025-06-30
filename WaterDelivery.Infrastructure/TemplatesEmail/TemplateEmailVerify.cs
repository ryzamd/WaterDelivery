using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaterDelivery.Infrastructure.TemplatesEmail
{
    public class TemplateEmailVerify
    {
        public static string GetEmailTemplate(string otp)
        {
            return $@"
            <!DOCTYPE html>
            <html lang=""vi"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Xác thực tài khoản</title>
                <style>
                    * {{
                        margin: 0;
                        padding: 0;
                        box-sizing: border-box;
                    }}
        
                    body {{
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
                        line-height: 1.6;
                        color: #333333;
                        background-color: #f8fafc;
                    }}
        
                    .container {{
                        max-width: 600px;
                        margin: 0 auto;
                        background-color: #ffffff;
                        border-radius: 12px;
                        overflow: hidden;
                        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
                    }}
        
                    .header {{
                        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
                        padding: 40px 30px;
                        text-align: center;
                        color: white;
                    }}
        
                    .logo {{
                        font-size: 32px;
                        font-weight: bold;
                        margin-bottom: 10px;
                    }}
        
                    .header-subtitle {{
                        font-size: 16px;
                        opacity: 0.9;
                    }}
        
                    .content {{
                        padding: 40px 30px;
                        text-align: center;
                    }}
        
                    .welcome-text {{
                        font-size: 24px;
                        font-weight: 600;
                        color: #2d3748;
                        margin-bottom: 20px;
                    }}
        
                    .description {{
                        font-size: 16px;
                        color: #718096;
                        margin-bottom: 30px;
                        line-height: 1.8;
                    }}
        
                    .otp-container {{
                        background: linear-gradient(135deg, #f7fafc 0%, #edf2f7 100%);
                        border: 2px solid #e2e8f0;
                        border-radius: 12px;
                        padding: 30px;
                        margin: 30px 0;
                        text-align: center;
                    }}
        
                    .otp-label {{
                        font-size: 14px;
                        color: #718096;
                        text-transform: uppercase;
                        letter-spacing: 1px;
                        margin-bottom: 10px;
                        font-weight: 600;
                    }}
        
                    .otp-code {{
                        font-size: 36px;
                        font-weight: bold;
                        color: #667eea;
                        letter-spacing: 8px;
                        font-family: 'Courier New', monospace;
                        margin: 10px 0;
                        text-shadow: 0 2px 4px rgba(102, 126, 234, 0.2);
                    }}
        
                    .otp-note {{
                        font-size: 14px;
                        color: #e53e3e;
                        font-weight: 500;
                        margin-top: 15px;
                    }}
        
                    .instructions {{
                        background-color: #f7fafc;
                        border-left: 4px solid #667eea;
                        padding: 20px;
                        margin: 30px 0;
                        border-radius: 0 8px 8px 0;
                    }}
        
                    .instructions h3 {{
                        color: #2d3748;
                        margin-bottom: 10px;
                        font-size: 18px;
                    }}
        
                    .instructions p {{
                        color: #718096;
                        margin-bottom: 8px;
                    }}
        
                    .footer {{
                        background-color: #f7fafc;
                        padding: 30px;
                        text-align: center;
                        border-top: 1px solid #e2e8f0;
                    }}
        
                    .footer-text {{
                        font-size: 14px;
                        color: #718096;
                        margin-bottom: 15px;
                    }}
        
                    .social-links {{
                        margin: 20px 0;
                    }}
        
                    .social-links a {{
                        display: inline-block;
                        margin: 0 10px;
                        padding: 10px;
                        background-color: #667eea;
                        color: white;
                        text-decoration: none;
                        border-radius: 50%;
                        width: 40px;
                        height: 40px;
                        line-height: 20px;
                    }}
        
                    .warning {{
                        background-color: #fed7d7;
                        border: 1px solid #feb2b2;
                        color: #c53030;
                        padding: 15px;
                        border-radius: 8px;
                        margin: 20px 0;
                        font-size: 14px;
                    }}
        
                    @media (max-width: 600px) {{
                        .container {{
                            margin: 10px;
                            border-radius: 8px;
                        }}
            
                        .header, .content, .footer {{
                            padding: 20px;
                        }}
            
                        .otp-code {{
                            font-size: 28px;
                            letter-spacing: 4px;
                        }}
            
                        .welcome-text {{
                            font-size: 20px;
                        }}
                    }}
                </style>
            </head>
            <body>
                <div class=""container"">
                    <!-- Header -->
                    <div class=""header"">
                        <div class=""logo"">WaterDelivery</div>
                        <div class=""header-subtitle"">Dịch vụ giao nước tận nơi</div>
                    </div>
        
                    <!-- Content -->
                    <div class=""content"">
                        <h1 class=""welcome-text"">Chào mừng bạn đến với WaterDelivery!</h1>
            
                        <p class=""description"">
                            Cảm ơn bạn đã đăng ký tài khoản. Để hoàn tất quá trình đăng ký, 
                            vui lòng sử dụng mã xác thực bên dưới:
                        </p>
            
                        <!-- OTP Container -->
                        <div class=""otp-container"">
                            <div class=""otp-label"">Mã xác thực</div>
                            <div class=""otp-code"">{otp}</div>
                            <div class=""otp-note"">Mã có hiệu lực trong 5 phút</div>
                        </div>
            
                        <!-- Instructions -->
                        <div class=""instructions"">
                            <h3>Hướng dẫn sử dụng:</h3>
                            <p>1. Sao chép mã xác thực ở trên</p>
                            <p>2. Quay lại ứng dụng WaterDelivery</p>
                            <p>3. Nhập mã vào ô xác thực</p>
                            <p>4. Nhấn ""Xác nhận"" để hoàn tất</p>
                        </div>
            
                        <!-- Security Warning -->
                        <div class=""warning"">
                            <strong>Bảo mật:</strong> Không chia sẻ mã này với bất kỳ ai. 
                            Đội ngũ WaterDelivery sẽ không bao giờ yêu cầu mã xác thực qua điện thoại.
                        </div>
                    </div>
        
                    <!-- Footer -->
                    <div class=""footer"">
                        <p class=""footer-text"">
                            Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email này.
                        </p>
            
                        <div class=""social-links"">
                            <a href=""#"" title=""Facebook"">📘</a>
                            <a href=""#"" title=""Instagram"">📷</a>
                            <a href=""#"" title=""Support"">📞</a>
                        </div>
            
                        <p class=""footer-text"">
                            © 2025 WaterDelivery. Tất cả quyền được bảo lưu.<br>
                            📧 Email: support@waterdelivery.com | 📱 Hotline: 1900-xxxx
                        </p>
                    </div>
                </div>
            </body>
            </html>";
        }
    }
}
