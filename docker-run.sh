#!/bin/bash

# Docker Build and Run Script for Password Manager Application

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

print_usage() {
    echo "Usage: $0 [COMMAND] [OPTIONS]"
    echo ""
    echo "Commands:"
    echo "  build-api       Build the API container"
    echo "  build-web       Build the Web container"
    echo "  build-maui      Build the MAUI Android container"
    echo "  build-all       Build all containers"
    echo "  run-api         Run the API container"
    echo "  run-web         Run the Web container"
    echo "  run-all         Run all containers with docker-compose"
    echo "  run-ci          Run all containers for CI environment"
    echo "  stop            Stop all containers"
    echo "  clean           Clean all containers and images"
    echo "  logs            Show logs for all containers"
    echo ""
    echo "Database Options (use with run-all):"
    echo "  --mysql         Use MySQL database"
    echo "  --sqlserver     Use SQL Server database"
    echo "  --postgres      Use PostgreSQL database (default)"
    echo ""
    echo "Examples:"
    echo "  $0 build-all"
    echo "  $0 run-all"
    echo "  $0 run-all --mysql"
    echo "  $0 run-ci"
    echo "  $0 logs"
}

log() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

build_api() {
    log "Building Password Manager API container..."
    docker build --target runtime-api -t password-manager-api:latest .
}

build_web() {
    log "Building Password Manager Web container..."
    docker build --target runtime-web -t password-manager-web:latest .
}

build_maui() {
    log "Building Password Manager MAUI Android container..."
    docker build --target build-maui-android --build-arg BUILD_CONFIGURATION=Release --build-arg MAUI_TARGET_FRAMEWORK=net9.0-android -t password-manager-maui-android:latest .
}

build_all() {
    log "Building all containers..."
    docker-compose build
}

run_api() {
    log "Running Password Manager API container..."
    docker run -d --name password-manager-api -p 5000:80 password-manager-api:latest
}

run_web() {
    log "Running Password Manager Web container..."
    docker run -d --name password-manager-web -p 5002:80 password-manager-web:latest
}

run_all() {
    local db_profile=""
    
    case "$2" in
        --mysql)
            db_profile="--profile mysql"
            log "Using MySQL database"
            ;;
        --sqlserver)
            db_profile="--profile sqlserver"
            log "Using SQL Server database"
            ;;
        --postgres|"")
            log "Using PostgreSQL database (default)"
            ;;
        *)
            warn "Unknown database option: $2. Using PostgreSQL (default)"
            ;;
    esac
    
    log "Starting all containers with docker-compose..."
    docker-compose up -d $db_profile
}

run_ci() {
    log "Starting all containers for CI environment..."
    docker-compose -f docker-compose.ci.yml up -d
}

stop_containers() {
    log "Stopping all containers..."
    docker-compose down
}

clean_containers() {
    log "Cleaning all containers and images..."
    docker-compose down --rmi all --volumes --remove-orphans
    docker system prune -f
}

show_logs() {
    log "Showing logs for all containers..."
    docker-compose logs -f
}

# Main script logic
case "$1" in
    build-api)
        build_api
        ;;
    build-web)
        build_web
        ;;
    build-maui)
        build_maui
        ;;
    build-all)
        build_all
        ;;
    run-api)
        run_api
        ;;
    run-web)
        run_web
        ;;
    run-all)
        run_all "$@"
        ;;
    run-ci)
        run_ci
        ;;
    stop)
        stop_containers
        ;;
    clean)
        clean_containers
        ;;
    logs)
        show_logs
        ;;
    -h|--help)
        print_usage
        ;;
    *)
        error "Unknown command: $1"
        print_usage
        exit 1
        ;;
esac

log "Operation completed successfully!"
