# DfE Service Manual

A comprehensive service manual for Digital, Data and Technology teams in the Department for Education.

## About

The DfE Service Manual provides standards, guidance, patterns, and resources to help teams design and build consistent, accessible, user-centred digital services. It consolidates content from multiple existing manuals into a single, unified resource.

## Content types

The Service Manual includes the following content types:

- **Standards** - Mandatory rules and requirements that teams must follow
- **Guidance** - Practical advice and how-to information
- **Patterns** - Reusable design solutions for common problems
- **Tools** - Software, templates, and resources
- **Processes** - Step-by-step workflows for governance and assurance
- **Case Studies** - Examples of how teams have built or improved services
- **Roles** - Information about digital roles and professions
- **Principles** - High-level values that guide decision-making
- **Lifecycle Stages** - Phases of service development (Discovery, Alpha, Beta, Live, Retirement)
- **Community** - Information about communities of practice and support
- **News** - Updates and announcements

## Technology

This is an ASP.NET Core 8.0 MVC application using:

- C# and Razor views
- GOV.UK Design System for styling (following British English and sentence case)
- DfE Frontend components
- Inter typeface for typography
- Strapi CMS for content management (to be integrated)

## Setup

### Prerequisites

- .NET 8.0 SDK
- Strapi CMS (when ready)

### Running locally

1. Clone the repository
2. Navigate to the project directory
3. Run the application:

```bash
dotnet run
```

The application will start on `http://localhost:5000` by default.

### Configuration

Application settings can be configured in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "CmsApi": {
    "BaseUrl": "http://localhost:1337/",
    "ApiToken": ""
  }
}
```

## CMS Integration

The application is designed to work with a Strapi CMS. Currently, mock data is returned by the `ContentApiService` until the CMS is built and configured.

### Setting up Strapi

When ready to integrate with Strapi:

1. Install and configure Strapi 5
2. Create content types matching the models in `/Models`
3. Configure API tokens in Strapi
4. Update `appsettings.json` with the Strapi API URL and token
5. Update `ContentApiService.cs` to make real API calls instead of returning mock data

## Project structure

```
ServiceManual/
├── Controllers/          # MVC controllers for each content type
├── Models/              # Content type models
├── Services/            # API service interfaces and implementations
├── Views/               # Razor view templates
│   ├── Home/           # Homepage and error pages
│   ├── Standard/       # Standard detail and list views
│   ├── Guidance/       # Guidance detail and list views
│   ├── Lifecycle/      # Lifecycle stage views
│   ├── News/           # News article views
│   └── Shared/         # Shared layout and components
├── wwwroot/            # Static assets
│   ├── css/           # Stylesheets
│   ├── js/            # JavaScript files
│   └── images/        # Images and logos
└── Program.cs          # Application entry point
```

## Visual Design

The Service Manual uses an engaging, modern design while maintaining GOV.UK standards:

### **Page Mastheads**
- Full-width gradient backgrounds (DfE blue gradient)
- Category badges and publication dates
- Clear content hierarchy

### **Download Artifacts**
- Card-based grid layout
- Hover effects and animations
- File type badges (PDF, DOCX, ZIP, etc.)
- Displayed as panels within main content area

### **Phase Tags**
Color-coded by lifecycle stage:
- **Discovery**: Purple (#912b88)
- **Alpha**: Red (#d4351c)
- **Beta**: Orange (#f47738)
- **Live**: Green (#00703c)
- **Retirement**: Grey (#505a5f)

### **Responsive Design**
- Mobile-first approach
- Adapts to all screen sizes
- Touch-friendly interface

## Content guidelines

All content should follow:

- British English spelling and grammar
- Sentence case for headings and titles
- GOV.UK content design principles
- Plain language standards
- Accessibility requirements (WCAG 2.2 AA)

## Contributing

Content should be managed through the Strapi CMS when available. For technical changes to the application:

1. Create a feature branch
2. Make your changes
3. Test locally
4. Submit a pull request

## Licence

All content is available under the Open Government Licence v3.0, except where otherwise stated.

