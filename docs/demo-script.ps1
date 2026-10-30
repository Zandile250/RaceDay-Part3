# RaceDay API demo - run while the containers are up (docker compose up --build -d)
# Each line prints OK (green) or the HTTP status code (yellow). Compare with the "expect" text.
$base = "http://localhost:8080/api"
$n = Get-Random -Maximum 99999
$orgS  = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$partS = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$anonS = New-Object Microsoft.PowerShell.Commands.WebRequestSession

function Call($label, $method, $path, $body, $session) {
  try {
    $a = @{ Method = $method; Uri = "$base$path"; WebSession = $session; ContentType = "application/json" }
    if ($body) { $a.Body = ($body | ConvertTo-Json) }
    $r = Invoke-RestMethod @a
    Write-Host "$label -> OK" -ForegroundColor Green
    return $r
  } catch {
    Write-Host "$label -> $([int]$_.Exception.Response.StatusCode)" -ForegroundColor Yellow
  }
}

# --- Registration and login ---
Call "1. Register organiser (correct code)    expect OK " Post "/auth/register" @{fullName="Olivia Organiser";email="org$n@example.com";password="Password123";role="Organiser";organiserInviteCode="RACEDAY-ORG-2026"} $anonS | Out-Null
Call "2. Register organiser (wrong code)      expect 403" Post "/auth/register" @{fullName="Bad Actor";email="bad$n@example.com";password="Password123";role="Organiser";organiserInviteCode="WRONG"} $anonS | Out-Null
Call "3. Register same email again            expect 409" Post "/auth/register" @{fullName="Olivia Organiser";email="org$n@example.com";password="Password123";role="Organiser";organiserInviteCode="RACEDAY-ORG-2026"} $anonS | Out-Null
Call "4. Register participant                 expect OK " Post "/auth/register" @{fullName="Sam Runner";email="sam$n@example.com";password="Password123";role="Participant"} $anonS | Out-Null
Call "5. Register participant #2              expect OK " Post "/auth/register" @{fullName="Thandi Nkosi";email="thandi$n@example.com";password="Password123";role="Participant"} $anonS | Out-Null
Call "6. Login with wrong password            expect 401" Post "/auth/login" @{email="org$n@example.com";password="nope-nope"} $anonS | Out-Null
Call "7. Organiser login                      expect OK " Post "/auth/login" @{email="org$n@example.com";password="Password123"} $orgS | Out-Null
Call "8. Participant login                    expect OK " Post "/auth/login" @{email="sam$n@example.com";password="Password123"} $partS | Out-Null

# --- Profile (authenticated only) ---
Call "9. Profile without logging in           expect 401" Get "/users/me" $null $anonS | Out-Null
Call "10. Participant views own profile       expect OK " Get "/users/me" $null $partS | Out-Null

# --- Events and categories ---
$ev = Call "11. Organiser creates event           expect OK " Post "/events" @{eventName="Soweto 10km";description="Community fun run";eventDate="2027-03-14T07:00:00";location="Soweto";routeInfo="Out and back";distanceKm=10;eventType="Run"} $orgS
Call "12. Participant creates event           expect 403" Post "/events" @{eventName="Illegal";eventDate="2027-03-14T07:00:00";location="x";distanceKm=5;eventType="Run"} $partS | Out-Null
Call "13. Public can list events (no login)   expect OK " Get "/events" $null $anonS | Out-Null
$cat = Call "14. Organiser adds category          expect OK " Post "/events/$($ev.eventId)/categories" @{categoryName="10km";distanceKm=10;price=100;maxParticipants=500} $orgS

# --- Enrolments ---
$enr = Call "15. Participant enrols               expect OK " Post "/enrolments" @{categoryId=$cat.categoryId} $partS
Call "16. Participant enrols again            expect 409" Post "/enrolments" @{categoryId=$cat.categoryId} $partS | Out-Null
Call "17. Organiser uses participant route    expect 403" Get "/enrolments/me" $null $orgS | Out-Null
Call "18. Organiser views event roster        expect OK " Get "/events/$($ev.eventId)/enrolments" $null $orgS | Out-Null

# --- Results ---
Call "19. Organiser captures result           expect OK " Post "/results" @{enrolmentId=$enr.enrolmentId;finishTime="00:52:30";position=1;status="Finished"} $orgS | Out-Null
Call "20. Participant captures a result       expect 403" Post "/results" @{enrolmentId=$enr.enrolmentId;finishTime="00:40:00";position=1;status="Finished"} $partS | Out-Null
Call "21. Participant views own results       expect OK " Get "/results/me" $null $partS | Out-Null
Call "22. Public views event results          expect OK " Get "/events/$($ev.eventId)/results" $null $anonS | Out-Null
